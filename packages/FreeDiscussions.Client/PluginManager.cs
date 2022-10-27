using FreeDiscussions.Client;
using FreeDiscussions.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Text;

namespace FreeDiscussions
{
    public class PluginManager
    {
        private string pluginPath = Path.Combine(Environment.CurrentDirectory, "plugins");
        private AggregateCatalog catalog = new AggregateCatalog();

        public PluginManager()
        {
            if (!Directory.Exists(pluginPath))
            {
                Directory.CreateDirectory(pluginPath);
            }
        }

        public void Setup()
        {
            try
            {
                catalog.Catalogs.Add(new AssemblyCatalog(typeof(PluginManager).Assembly));
                var container = new CompositionContainer(catalog);
                container.ComposeParts(PluginContainer.Instance);

                // load plugins from "./plugins/MyPlugin" directory
                foreach (var dir in Directory.GetDirectories(pluginPath))
                {
                    var countBefore = PluginContainer.Instance.Plugins.Count;
                    try { 
                    catalog.Catalogs.Add(new DirectoryCatalog(dir, "*.dll"));

                    for (var i = countBefore; i != PluginContainer.Instance.Plugins.Count; i++)
                    {
                        PluginContainer.Instance.Plugins[i].Path = dir;
                     
                    }
                    } catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                
                foreach (var plugin in PluginContainer.Instance.Plugins)
                {
                   var pluginConfig = AppStart.Config.plugins.FirstOrDefault(x => x.Name.Equals(plugin.Guid));
                   plugin.SetConfig(pluginConfig.config);
                }
                
            }
            catch (CompositionException compositionException)
            {
                Console.WriteLine(compositionException.ToString());
            }
        }
    }
}
