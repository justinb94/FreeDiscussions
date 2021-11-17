using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace FreeDiscussions.Client.UI
{
    public abstract class Panel : Control
    {
        private Action onClose;

        public Panel(Action onClose)
        {
            this.onClose = onClose;
        }
    }
}
