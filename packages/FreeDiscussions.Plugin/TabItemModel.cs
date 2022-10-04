using System.Windows.Controls;

namespace FreeDiscussions.Plugin
{
    /// <summary>
    /// TabItemModel
    /// </summary>
    public class TabItemModel
    {
        /// <summary>
        /// The id of the Tab
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The text of the tab (will get shown in the header)
        /// </summary>
        public string HeaderText { get; set; }
        /// <summary>
        /// The icon path of the tab
        /// </summary>
        public string IconPath { get; set; }
        /// <summary>
        /// The UserControl of the Tab
        /// </summary>
        public UserControl Control { get; set; }
        /// <summary>
        /// Closes the tab
        /// </summary>
        public DelegateCommand<string> Close { get; set; }
        /// <summary>
        /// Contructor
        /// </summary>
        /// <param name="id">The unique identiefier of the tab</param>
        public TabItemModel(string id)
        {
            this.Id = id;
        }
    }
}
