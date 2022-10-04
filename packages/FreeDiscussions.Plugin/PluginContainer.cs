using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace FreeDiscussions.Plugin
{
    /// <summary>
    /// A container that holds all the plugins
    /// </summary>
    public class PluginContainer
    {
        public static PluginContainer Instance = new PluginContainer();

        [ImportMany(typeof(IPlugin), AllowRecomposition = true)]
        public IList<IPlugin> Plugins;

        PluginContainer()
        {
            this.Plugins = new List<IPlugin>();
        }
    }
}
