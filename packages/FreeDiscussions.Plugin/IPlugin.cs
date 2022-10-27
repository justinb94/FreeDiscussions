using System.Collections.Generic;
using System.Threading.Tasks;

namespace FreeDiscussions.Plugin
{
    /// <summary>
    /// Plugin interface
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Type of the panel
        /// </summary>
        public PanelType Type{ get; set; }
        /// <summary>
        /// Unique plugin identifier
        /// </summary>
        public string Guid { get; set; }
        /// <summary>
        /// Name of the plugin
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Path to the icon
        /// </summary>
        public string IconPath { get; set; }
        /// <summary>
        /// Plugin path
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// Set by the client
        /// </summary>
        bool IsOffical { get; set; }

        /// <summary>
        /// Creates a tab item
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public Task<TabItemModel> Create(params object[] args);
        /// <summary>
        /// Initializes the plugin
        /// </summary>
        /// <returns></returns>
        public Task Init();

        /// <summary>
        /// Set by the client
        /// </summary>
        void SetConfig(Dictionary<string, string> config);
    }
}
