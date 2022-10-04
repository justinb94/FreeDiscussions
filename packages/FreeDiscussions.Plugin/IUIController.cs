using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FreeDiscussions.Plugin
{
    /// <summary>
    /// UIController interface
    /// </summary>
    public interface IUIController
    {
        /// <summary>
        /// Open a panel
        /// </summary>
        /// <param name="type"></param>
        /// <param name="tab"></param>
        /// <returns></returns>
        Task OpenPanel(PanelType type, TabItemModel tab);
        /// <summary>
        /// Close a panel tiwht the given id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task ClosePanel(string id);
        /// <summary>
        /// Hide the Applications Sidebar
        /// </summary>
        void HideSidebar();
        /// <summary>
        /// Show the Applications Sidebar
        /// </summary>
        void ShowSidebar();
        /// <summary>
        /// Updates the Plugins
        /// </summary>
        void UpdatePlugins();
        /// <summary>
        /// Opens the Settings Panel
        /// </summary>
        void ShowSettingsPanel();
    }
}
