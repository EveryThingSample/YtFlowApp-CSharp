use serde::Deserialize;

use crate::config::factory::*;
use crate::config::*;
#[cfg(feature = "plugins")]
use crate::plugin::obfs::simple_http;

#[derive(Deserialize)]
pub struct HttpObfsServerFactory<'a> {
    next: &'a str,
}

#[cfg_attr(not(feature = "plugins"), allow(dead_code))]
#[derive(Deserialize)]
pub struct HttpObfsClientFactory<'a> {
    host: &'a str,
    path: &'a str,
    next: &'a str,
}

impl<'de> HttpObfsServerFactory<'de> {
    pub(in super::super) fn parse(plugin: &'de Plugin) -> ConfigResult<ParsedPlugin<'de, Self>> {
        let Plugin { name, param, .. } = plugin;
        let config: Self = parse_param(name, param)?;
        let next = config.next;
        Ok(ParsedPlugin {
            factory: config,
            requires: vec![Descriptor {
                descriptor: next,
                r#type: AccessPointType::STREAM_HANDLER,
            }],
            provides: vec![Descriptor {
                descriptor: name.to_string() + ".tcp",
                r#type: AccessPointType::STREAM_HANDLER,
            }],
            resources: vec![],
        })
    }
}

impl<'de> HttpObfsClientFactory<'de> {
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

impl<'de> Factory for HttpObfsServerFactory<'de> {
    #[cfg(feature = "plugins")]
    fn load(&mut self, plugin_name: String, set: &mut PartialPluginSet) -> LoadResult<()> {
        use crate::plugin::reject::RejectHandler;

        let factory = Arc::new_cyclic(|weak| {
            set.stream_handlers
                .insert(plugin_name.clone() + ".tcp", weak.clone() as _);
            let next = match set.get_or_create_stream_handler(plugin_name.clone(), self.next) {
                Ok(next) => next,
                Err(e) => {
                    set.errors.push(e);
                    Arc::downgrade(&(Arc::new(RejectHandler)))
                }
            };

            simple_http::SimpleHttpHandler::new(next)
        });
        set.fully_constructed
            .stream_handlers
            .insert(plugin_name + ".tcp", factory);
        Ok(())
    }
}

impl<'de> Factory for HttpObfsClientFactory<'de> {
    #[cfg(feature = "plugins")]
    fn load(&mut self, plugin_name: String, set: &mut PartialPluginSet) -> LoadResult<()> {
        use crate::plugin::null::Null;

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

            simple_http::SimpleHttpOutbound::new(self.path.as_bytes(), self.host.as_bytes(), next)
        });
        set.fully_constructed
            .stream_outbounds
            .insert(plugin_name + ".tcp", factory);
        Ok(())
    }
}
