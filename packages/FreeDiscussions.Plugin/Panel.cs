using FreeDiscussions.Plugin;
using System;
using System.Windows.Controls;

namespace FreeDiscussions.Plugin
{
    public class Panel : UserControl
    {
        public Action onClose;

        public Panel(Action onClose)
        {
            this.onClose = onClose;
        }
    }
}
