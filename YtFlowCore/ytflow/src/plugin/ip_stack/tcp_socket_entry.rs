use std::sync::atomic::{AtomicI64, Ordering};
use std::sync::{Arc, Mutex, MutexGuard};
use std::time::Instant;

use smoltcp::iface::SocketHandle;
use smoltcp::socket::tcp::Socket as TcpSocket;

use super::*;

pub(super) struct TcpSocketEntry {
    pub(super) socket_handle: SocketHandle,
    pub(super) local_endpoint: SocketAddr,
    pub(super) stack: Arc<Mutex<IpStackInner>>,
    pub(super) most_recent_scheduled_poll: Arc<AtomicI64>,
}

impl TcpSocketEntry {
    pub fn lock(&self) -> SocketEntryGuard<'_> {
        SocketEntryGuard {
            entry: self,
            guard: self.stack.lock().unwrap(),
        }
    }
}

pub(super) struct SocketEntryGuard<'s> {
    pub(super) entry: &'s TcpSocketEntry,
    pub(super) guard: MutexGuard<'s, IpStackInner>,
}

impl<'s> SocketEntryGuard<'s> {
    pub fn with_socket<R>(&mut self, f: impl FnOnce(&mut TcpSocket) -> R) -> R {
        let handle = self.entry.socket_handle;
        let socket = self.guard.socket_set.get_mut::<TcpSocket<'static>>(handle);
        f(socket)
    }

    pub fn poll(&mut self) {
        let now = Instant::now();
        let Self { entry, guard } = self;
        let TcpSocketEntry {
            stack,
            most_recent_scheduled_poll,
            ..
        } = entry;
        let IpStackInner {
            netif,
            dev,
            socket_set,
            ..
        } = &mut **guard;
        let _ = netif.poll(now.into(), dev, socket_set);
        if let Some(delay) = netif.poll_delay(now.into(), socket_set) {
            let scheduled_poll_milli = (smoltcp::time::Instant::from(now) + delay).total_millis();
            if scheduled_poll_milli >= most_recent_scheduled_poll.load(Ordering::Relaxed) {
                return;
            }
            // TODO: CAS spin loop
            most_recent_scheduled_poll.store(scheduled_poll_milli, Ordering::Relaxed);

            tokio::spawn(schedule_repoll(
                stack.clone(),
                now + Duration::from(delay),
                Arc::clone(most_recent_scheduled_poll),
            ));
        }
    }
}
