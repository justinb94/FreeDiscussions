using FreeDiscussions.Client.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Threading;

namespace FreeDiscussions.Client.UI
{
    /// <summary>
    /// Interaction logic for NewsgroupsContentPanel.xaml
    /// </summary>
    public partial class NewsgroupsContentPanel : Panel
    {
        static object _syncLock = new object();
        ObservableCollection<ArticleModel> articles = new ObservableCollection<ArticleModel>();
        string SelectedNewsgroup;
        long SelectedGroupHigh = 0;
        long SelectedGroupLow = 0;

        public NewsgroupsContentPanel(string newsgroup, Action onClose) : base(onClose)
        {
            InitializeComponent();
            SelectedNewsgroup = newsgroup;
        }

        private void NewsgroupContentListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private async Task LoadNewsgroup(string ng)
        {
            var credentials = SettingsModel.GetCredentials();
            var settings = SettingsModel.Read();

            //using (var client = new Client())
            //{
            //    await client.Connect(settings.Hostname, settings.Port, settings.SSL, credentials.Username, credentials.Password);

            //    var group = client.nntp.Group(ng);

            //    DispatcherTimer dispatcherTimer = new DispatcherTimer();
            //    dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            //    dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            //    dispatcherTimer.Start();

            //    BindingOperations.EnableCollectionSynchronization(articles, _syncLock);
            //    articles.Clear();

            //    SelectedGroupHigh = group.Group.HighWaterMark;
            //    SelectedGroupLow = group.Group.LowWaterMark;

            //    Task.Factory.StartNew(() => GetFirst());
            //}
        }

        async Task GetFirst()
        {
            var credentials = SettingsModel.GetCredentials();
            var settings = SettingsModel.Read();

            //using (var client = new Client())
            //{
            //    await client.Connect(settings.Hostname, settings.Port, settings.SSL, credentials.Username, credentials.Password);

            //    client.nntp.Group(SelectedNewsgroup);

            //    var e = 0;
            //    for (var i = SelectedGroupHigh; i != SelectedGroupLow; i--)
            //    {
            //        var a = client.nntp.Head(i);
            //        if (a.Code == 221)
            //        {
            //            articles.Add(ArticleFactory(a));
            //        }

            //        e++;

            //        if (e > 20)
            //        {
            //            SelectedGroupHigh = i - 1;
            //            break;
            //        }
            //    }
            //}
        }

        protected void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            NewsgroupContentListBox.ItemsSource = articles;
        }
    }
}
