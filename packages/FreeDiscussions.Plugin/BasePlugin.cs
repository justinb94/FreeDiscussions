using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace FreeDiscussions.Plugin
{
    /// <summary>
    /// Plugin Base
    /// </summary>
    public abstract class BasePlugin : IPlugin
    {
        /// <summary>
        /// Type of the panel
        /// </summary>
        public abstract PanelType Type { get; set; }
        /// <summary>
        /// Unique plugin identifier
        /// </summary>
        public abstract string Guid { get; set; }
        /// <summary>
        /// Name of the plugin
        /// </summary>
        public abstract string Name { get; set; }
        /// <summary>
        /// Path to the icon
        /// </summary>
        public abstract string IconPath { get; set; }
        /// <summary>
        /// Plugin path
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Set by the client
        /// </summary>
        public bool IsOffical { get; set; }

        /// <summary>
        /// Creates a tab item
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public abstract Task<TabItemModel> Create(params object[] args);
        /// <summary>
        /// Initializes the plugin
        /// </summary>
        /// <returns></returns>
        public virtual Task Init()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Set by the client
        /// </summary>
        public virtual void SetConfig(Dictionary<string, string> config) { }
    }
}