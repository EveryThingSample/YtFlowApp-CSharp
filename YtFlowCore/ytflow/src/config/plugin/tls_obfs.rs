use serde::Deserialize;

use crate::config::factory::*;
use crate::config::*;

#[cfg_attr(not(feature = "plugins"), allow(dead_code))]
#[derive(Deserialize)]
pub struct TlsObfsClientFactory<'a> {
    host: &'a str,
    next: &'a str,
}

impl<'de> TlsObfsClientFactory<'de> {
    pub(in super::super) fn parse(plugin: &'de Plugin) -> ConfigResult<ParsedPlugin<'de, Self>> {
        let Plugin { name, param, .. } = plugin;
        let config: Self = parse_param(name, param)?;
        let next = config.next;
        Ok(ParsedPlugin {
            factory: config,
            requires: vec![Descriptor {
                descriptor: next,
                r#type: AccessPointType::STREAM_OUTBOUND_FACTORY,
            }],
            provides: vec![Descriptor {
                descriptor: name.to_string() + ".tcp",
                r#type: AccessPointType::STREAM_OUTBOUND_FACTORY,
            }],
            resources: vec![],
        })
    }
}

impl<'de> Factory for TlsObfsClientFactory<'de> {
    #[cfg(feature = "plugins")]
    fn load(&mut self, plugin_name: String, set: &mut PartialPluginSet) -> LoadResult<()> {
        use crate::plugin::null::Null;
        use crate::plugin::obfs::simple_tls;

        let factory = Arc::new_cyclic(|weak| {
            set.stream_outbounds
                .insert(plugin_name.clone() + ".tcp", weak.clone() as _);
            let next = match set.get_or_create_stream_outbound(plugin_name.clone(), self.next) {
                Ok(next) => next,
                Err(e) => {
                    set.errors.push(e);
                    Arc::downgrade(&(Arc::new(Null)))
                }
            };

            simple_tls::SimpleTlsOutbound::new(self.host.as_bytes(), next)
        });
        set.fully_constructed
            .stream_outbounds
            .insert(plugin_name + ".tcp", factory);
        Ok(())
    }
}
