using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FreeDiscussions.Plugins.Download
{
    /// <summary>
    /// Interaction logic for Panel.xaml
    /// </summary>
    public partial class Panel : FreeDiscussions.Plugin.Panel
    {
        public Panel(Action onClose) : base(onClose)
        {
            InitializeComponent();
        }
    }
}
