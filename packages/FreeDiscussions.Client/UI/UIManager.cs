using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeDiscussions.Client.UI
{
    public class UIManager
    {
        public static UIManager Instance;

        private IUIManager _control;

        public UIManager(IUIManager control)
        {
            this._control = control;
            UIManager.Instance = this;

            Log.Information("UIManager initialized");
        }

        public void OpenOrSelectNewsgroup(string name)
        {
            this._control.OpenOrSelectNewsgroup(name);
        }
    }

    public interface IUIManager
    {
        void OpenOrSelectNewsgroup(string name);
    }
}
