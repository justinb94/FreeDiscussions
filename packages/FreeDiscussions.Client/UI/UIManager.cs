using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeDiscussions.Client.UI
{
    public class UIManager
    {
        public static IUIManager Instance;
    }

    public interface IUIManager
    {
        void OpenOrSelectNewsgroup(string name);
        void ShowSettingsPanel();
        void ClosePanel(string name);
    }
}
