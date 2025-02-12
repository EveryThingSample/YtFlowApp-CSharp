use serde::Deserialize;

use crate::config::factory::*;
use crate::config::*;

fn default_request_timeout() -> u64 {
    100
}

#[cfg_attr(not(feature = "plugins"), allow(dead_code))]
#[derive(Clone, Deserialize)]
pub struct ForwardFactory<'a> {
    #[serde(default = "default_request_timeout")]
    request_timeout: u64,
    tcp_next: &'a str,
    udp_next: &'a str,
}

impl<'de> ForwardFactory<'de> {
    pub(in super::super) fn parse(plugin: &'de Plugin) -> ConfigResult<ParsedPlugin<'de, Self>> {
        let Plugin { name, param, .. } = plugin;
        let config: Self = parse_param(name, param)?;
        Ok(ParsedPlugin {
            factory: config.clone(),
            requires: vec![
                Descriptor {
                    descriptor: config.tcp_next,
                    r#type: AccessPointType::STREAM_OUTBOUND_FACTORY,
                },
                Descriptor {
                    descriptor: config.udp_next,
                    r#type: AccessPointType::DATAGRAM_SESSION_FACTORY,
                },
            ],
            provides: vec![
                Descriptor {
                    descriptor: name.to_string() + ".tcp",
                    r#type: AccessPointType::STREAM_HANDLER,
                },
                Descriptor {
                    descriptor: name.to_string() + ".udp",
                    r#type: AccessPointType::DATAGRAM_SESSION_HANDLER,
                },
            ],
            resources: vec![],
        })
    }
}

impl<'de> Factory for ForwardFactory<'de> {
    #[cfg(feature = "plugins")]
    fn load(&mut self, plugin_name: String, set: &mut PartialPluginSet) -> LoadResult<()> {
        use crate::plugin::forward;
        use crate::plugin::null::Null;

        let stat = forward::StatHandle::default();
        let tcp_factory = Arc::new_cyclic(|weak| {
            set.stream_handlers
                .insert(plugin_name.clone() + ".tcp", weak.clone() as _);
            let tcp_next =
                match set.get_or_create_stream_outbound(plugin_name.clone(), self.tcp_next) {
                    Ok(t) => t,
                    Err(e) => {
                        set.errors.push(e);
                        Arc::downgrade(&(Arc::new(Null)))
                    }
                };
            forward::StreamForwardHandler {
                outbound: tcp_next,
                request_timeout: self.request_timeout,
                stat: stat.clone(),
            }
        });
        let udp_factory = Arc::new_cyclic(|weak| {
            set.datagram_handlers
                .insert(plugin_name.clone() + ".udp", weak.clone() as _);
            let udp_next =
                match set.get_or_create_datagram_outbound(plugin_name.clone(), self.udp_next) {
                    Ok(u) => u,
                    Err(e) => {
                        set.errors.push(e);
                        Arc::downgrade(&(Arc::new(Null)))
                    }
                };
            forward::DatagramForwardHandler {
                outbound: udp_next,
                stat: stat.clone(),
            }
        });
        set.fully_constructed
            .stream_handlers
            .insert(plugin_name.clone() + ".tcp", tcp_factory);
        set.fully_constructed
            .datagram_handlers
            .insert(plugin_name.clone() + ".udp", udp_factory);
        set.control_hub.create_plugin_control(
            plugin_name,
            "forward",
            forward::Responder::new(stat),
        );
        Ok(())
    }
}
