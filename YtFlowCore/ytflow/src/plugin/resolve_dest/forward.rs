use std::collections::BTreeMap;
use std::net::IpAddr;
use std::sync::{Arc, Weak};
use std::task::{Context, Poll};

use futures::{future::BoxFuture, ready};

use crate::flow::*;

pub struct StreamForwardResolver {
    pub resolver: Weak<dyn Resolver>,
    pub next: Weak<dyn StreamHandler>,
}

pub struct DatagramForwardResolver {
    pub resolver: Weak<dyn Resolver>,
    pub next: Weak<dyn DatagramSessionHandler>,
}

fn handle_context(
    resolver: &Weak<dyn Resolver>,
    mut context: Box<FlowContext>,
    on_context: impl FnOnce(Box<FlowContext>) + Send + 'static,
) {
    let domain = match &mut context.remote_peer.host {
        HostName::DomainName(domain) => std::mem::take(domain),
        _ => return on_context(context),
    };
    let resolver = match resolver.upgrade() {
        Some(resolver) => resolver,
        None => return,
    };
    tokio::spawn(async move {
        context.remote_peer = super::try_resolve_forward(
            context.local_peer.is_ipv6(),
            resolver,
            domain,
            context.remote_peer.port,
        )
        .await;
        on_context(context);
        FlowResult::Ok(())
    });
}

impl StreamHandler for StreamForwardResolver {
    fn on_stream(&self, lower: Box<dyn Stream>, initial_data: Buffer, context: Box<FlowContext>) {
        let next = match self.next.upgrade() {
            Some(next) => next,
            None => return,
        };
        handle_context(&self.resolver, context, move |c| {
            next.on_stream(lower, initial_data, c)
        });
    }
}

impl DatagramSessionHandler for DatagramForwardResolver {
    fn on_session(&self, lower: Box<dyn DatagramSession>, context: Box<FlowContext>) {
        let next = match self.next.upgrade() {
            Some(next) => next,
            None => return,
        };
        let resolver = match self.resolver.upgrade() {
            Some(resolver) => resolver,
            None => return,
        };
        let old_remote_host = context.remote_peer.host.clone();
        handle_context(&self.resolver, context, move |c| {
            let mut reverse_mapping = BTreeMap::new();
            if let (HostName::DomainName(domain), HostName::Ip(resolved_ip)) =
                (old_remote_host, c.remote_peer.host.clone())
            {
                reverse_mapping.insert(resolved_ip, domain);
            }
            next.on_session(
                Box::new(DatagramForwardSession {
                    is_ipv6: c.local_peer.is_ipv6(),
                    resolver,
                    lower,
                    resolving: None,
                    reverse_mapping,
                }),
                c,
            )
        });
    }
}

struct DatagramForwardSession {
    is_ipv6: bool,
    resolver: Arc<dyn Resolver>,
    lower: Box<dyn DatagramSession>,
    resolving: Option<BoxFuture<'static, (DestinationAddr, Buffer)>>,
    reverse_mapping: BTreeMap<IpAddr, String>,
}

impl DatagramSession for DatagramForwardSession {
    fn poll_recv_from(&mut self, cx: &mut Context) -> Poll<Option<(DestinationAddr, Buffer)>> {
        if let Some(mut fut) = self.resolving.take() {
            return if let Poll::Ready(ret) = fut.as_mut().poll(cx) {
                Poll::Ready(Some(ret))
            } else {
                self.resolving = Some(fut);
                Poll::Pending
            };
        }
        let (dest, buf) = match ready!(self.lower.as_mut().poll_recv_from(cx)) {
            Some(ret) => ret,
            None => return Poll::Ready(None),
        };
        let (domain, port) = match dest {
            DestinationAddr {
                host: HostName::DomainName(domain),
                port,
            } => (domain, port),
            dest => return Poll::Ready(Some((dest, buf))),
        };
        let resolver = self.resolver.clone();
        let is_ipv6 = self.is_ipv6;
        self.resolving = Some(Box::pin(async move {
            (
                super::try_resolve_forward(is_ipv6, resolver, domain, port).await,
                buf,
            )
        }));
        self.poll_recv_from(cx)
    }

    fn poll_send_ready(&mut self, cx: &mut Context<'_>) -> Poll<()> {
        self.lower.as_mut().poll_send_ready(cx)
    }

    fn send_to(&mut self, mut remote_peer: DestinationAddr, buf: Buffer) {
        if let HostName::Ip(ip) = remote_peer.host {
            if let Some(domain) = self.reverse_mapping.get(&ip) {
                remote_peer.host = HostName::DomainName(domain.clone())
            }
        }
        self.lower.as_mut().send_to(remote_peer, buf)
    }

    fn poll_shutdown(&mut self, cx: &mut Context<'_>) -> Poll<FlowResult<()>> {
        self.lower.as_mut().poll_shutdown(cx)
    }
}
