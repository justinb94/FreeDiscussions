using FreeDiscussions.Client.Models;
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

        private void NewsgroupListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            var newsgroupName = e.AddedItems[0] as string;
            Context.Instance.UIManager.OpenPanel(Plugin.PanelLocation.Main, new Plugin.TabItemModel(newsgroupName)
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
