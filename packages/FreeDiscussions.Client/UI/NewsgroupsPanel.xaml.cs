using FreeDiscussions.Plugin;
using FreeDiscussions.Plugin.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for NewsgroupsPanel.xaml
    /// </summary>
    public partial class NewsgroupsPanel : Plugin.Panel
    {
        private ICollectionView _newsgroupFilter;

        public NewsgroupsPanel(Action onClose) : base(onClose)
        {
            InitializeComponent();

            var x = File.ReadAllText(System.IO.Path.Combine(Environment.CurrentDirectory, "NewsgroupCategories.json"));
            var j = JsonConvert.DeserializeObject<List<NewsgroupCategory>>(x);
            NewsgroupCategoryListBox.ItemsSource = j;
        }

        private void NewsgroupCategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;

            var item = e.AddedItems[0] as NewsgroupCategory;

            _newsgroupFilter = CollectionViewSource.GetDefaultView(item.Groups);
            this.NewsgroupListBox.ItemsSource = _newsgroupFilter;
            this.FilterNewsgroupsTextBox.Text = "";
        }


        private void FilterNewsgroupsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.NewsgroupListBox.ItemsSource == null) return;

            var view = (ICollectionView)this.NewsgroupListBox.ItemsSource;
            view.Filter = p => p.ToString().ToUpper().Contains(FilterNewsgroupsTextBox.Text.ToUpper());
        }

        private async void NewsgroupListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            var newsgroupName = e.AddedItems[0] as string;

            var alternativeNewsgroupViews = PluginContainer.Instance.Plugins.Where(x => x.Type == PanelType.NewsgroupView).ToList();
            if (alternativeNewsgroupViews.Count != 0)
            {
                var first = alternativeNewsgroupViews.First();
                var tabItem = await first.Create(newsgroupName);
                // we use the first we get here, maybe implement a picker window to present to the user
                tabItem.Close = new DelegateCommand<string>((o) =>
                {
                    Context.Instance.UIManager.ClosePanel(tabItem.HeaderText);
                });

                 await Context.Instance.UIManager.OpenPanel(Plugin.PanelType.Main, tabItem);
                return;
            }

            await Context.Instance.UIManager.OpenPanel(Plugin.PanelType.Main, new Plugin.TabItemModel(newsgroupName)
            {
                HeaderText = newsgroupName,
                IconPath = "/FreeDiscussions.Client;component/Resources/globe.svg",
                Control = new NewsgroupsContentPanel(newsgroupName, () => {
                    Context.Instance.UIManager.ClosePanel(newsgroupName);
                }),
                Close = new DelegateCommand<string>((o) =>
                {
                    Context.Instance.UIManager.ClosePanel(newsgroupName);
                })
            });
        }
    }
}
