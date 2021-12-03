using System.Windows.Controls;

namespace FreeDiscussions.Plugin
{
    public class TabItemModel
    {
        public string Id { get; set; }
        public string HeaderText { get; set; }
        public string IconPath { get; set; }
        public UserControl Control { get; set; }

        public DelegateCommand<string> Close { get; set; }

        public TabItemModel(string id)
        {
            this.Id = id;
        }
    }
}
