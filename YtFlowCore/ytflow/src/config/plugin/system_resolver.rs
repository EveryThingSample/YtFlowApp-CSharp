use crate::config::factory::*;
use crate::config::*;

#[derive(Clone)]
pub struct SystemResolverFactory;

impl SystemResolverFactory {
    pub(in super::super) fn parse(plugin: &Plugin) -> ConfigResult<ParsedPlugin<'_, Self>> {
        Ok(ParsedPlugin {
            factory: Self,
            requires: vec![],
            provides: vec![Descriptor {
                descriptor: plugin.name.to_string() + ".resolver",
                r#type: AccessPointType::RESOLVER,
            }],
            resources: vec![],
        })
    }
}

impl Factory for SystemResolverFactory {
    #[cfg(feature = "plugins")]
    fn load(&mut self, plugin_name: String, set: &mut PartialPluginSet) -> LoadResult<()> {
        use crate::plugin::system_resolver::SystemResolver;

        let resolver = Arc::new(SystemResolver::new());
        set.fully_constructed
            .resolver
            .insert(plugin_name + ".resolver", resolver);
        Ok(())
    }
}
