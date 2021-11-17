using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace FreeDiscussions.Client.UI
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
