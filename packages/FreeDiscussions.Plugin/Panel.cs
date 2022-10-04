using FreeDiscussions.Plugin;
using System;
using System.Windows.Controls;

namespace FreeDiscussions.Plugin
{
    /// <summary>
    /// Panel Base Class
    /// </summary>
    public class Panel : UserControl
    {
        public Action onClose;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="onClose">Should be called by the plugin if necessary</param>
        public Panel(Action onClose)
        {
            this.onClose = onClose;
        }
    }
}
