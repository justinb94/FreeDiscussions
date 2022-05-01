using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FreeDiscussions.Plugin
{
    public interface IUIController
    {
        Task OpenPanel(PanelType type, TabItemModel tab);
        Task ClosePanel(string id);
        void HideSidebar();
        void ShowSidebar();
        void UpdatePlugins();
        void ShowSettingsPanel();
    }
}
