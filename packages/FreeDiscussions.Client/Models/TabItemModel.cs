using System.Windows.Controls;

namespace FreeDiscussions.Client.Models
{
    public class TabItemModel
    {
        public string HeaderText { get; set; }
        public string HeaderImage { get; set; }
        public UserControl Control { get; set; }

        public DelegateCommand<string> Close { get; set; }
    }
}
