using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FreeDiscussions.Client.UI
{
    /// <summary>
    /// Interaction logic for NewsgroupsContentPanel.xaml
    /// </summary>
    public partial class NewsgroupsContentPanel : Panel
    {
        public NewsgroupsContentPanel(string newsgroup, Action onClose) : base(onClose)
        {
            InitializeComponent();
        }
    }
}
