use serde::Deserialize;
use serde_bytes::Bytes;

use crate::config::factory::*;
use crate::config::*;

#[cfg_attr(not(feature = "plugins"), allow(dead_code))]
#[derive(Clone, Deserialize)]
pub struct TrojanFactory<'a> {
    password: &'a Bytes,
    tls_next: &'a str,
}

impl<'de> TrojanFactory<'de> {
    pub(in super::super) fn parse(plugin: &'de Plugin) -> ConfigResult<ParsedPlugin<'de, Self>> {
        let Plugin { name, param, .. } = plugin;
        let config: Self = parse_param(name, param)?;
        Ok(ParsedPlugin {
            factory: config.clone(),
            requires: vec![Descriptor {
                descriptor: config.tls_next,
                r#type: AccessPointType::STREAM_OUTBOUND_FACTORY,
            }],
            provides: vec![
                Descriptor {
                    descriptor: name.to_string() + ".tcp",
                    r#type: AccessPointType::STREAM_OUTBOUND_FACTORY,
                },
                Descriptor {
                    descriptor: name.to_string() + ".udp",
                    r#type: AccessPointType::DATAGRAM_SESSION_FACTORY,
                },
            ],
            resources: vec![],
        })
    }
}

impl<'de> Factory for TrojanFactory<'de> {
    #[cfg(feature = "plugins")]
    fn load(&mut self, plugin_name: String, set: &mut PartialPluginSet) -> LoadResult<()> {
        use crate::plugin::null::Null;
        use crate::plugin::trojan;

        let factory = Arc::new_cyclic(|weak| {
            set.stream_outbounds
                .insert(plugin_name.clone() + ".tcp", weak.clone() as _);
            set.datagram_outbounds.insert(
                plugin_name.clone() + ".udp",
                // TODO: trojan udp
                Arc::downgrade(&Arc::new(Null)) as _,
            );
            let tls_next =
                match set.get_or_create_stream_outbound(plugin_name.clone(), self.tls_next) {
                    Ok(t) => t,
                    Err(e) => {
                        set.errors.push(e);
                        Arc::downgrade(&(Arc::new(Null) as _))
                    }
                };
            trojan::TrojanStreamOutboundFactory::new(self.password, tls_next)
        });
        set.fully_constructed
            .stream_outbounds
            .insert(plugin_name + ".tcp", factory);
        Ok(())
    }
}
