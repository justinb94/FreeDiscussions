using FreeDiscussions.Client.Factory;
using FreeDiscussions.Client.Models;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Usenet.Nntp.Responses;
using Usenet.Yenc;

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
        ArticleModel SelectedArticle;
        long SelectedGroupHigh = 0;
        long SelectedGroupLow = 0;

        public NewsgroupsContentPanel(string newsgroup, Action onClose) : base(onClose)
        {
            InitializeComponent();
            SelectedNewsgroup = newsgroup;
            LoadNewsgroup();
        }

        private void NewsgroupContentListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;

            _ = LoadMessage(e.AddedItems[0] as ArticleModel);
        }

        private async Task LoadMessage(ArticleModel article)
        {
            ArticleBody.Text = "Loading...";

            SelectedArticle = article;

            var client = await ConnectionManager.GetClient();
            var settings = SettingsModel.Read();
            client.Group(SelectedNewsgroup);

            try
            {

                var a = client.Article(article.MessageId);
                if (a.Article == null)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        ArticleBody.Text = "Message not found";
                    });
                    return;
                }

                try
                {
                    // decode the yEnc-encoded article
                    using (YencStream yencStream = YencStreamDecoder.Decode(a.Article.Body))
                    {
                        this.Dispatcher.Invoke(() =>
                            {
                                ArticleBody.Text = "This is a binary. Press Download to save on disk.";
                            });
                    }
                }
                catch (Exception ex)
                {
                    var articleWithBody = client.Body(article.MessageId);
                    var text = String.Join("\n", articleWithBody.Article.Body.ToList());
                    this.Dispatcher.Invoke(() =>
                    {
                        ArticleBody.Text = text;
                    });

                    // try uuencode 

                    // Text 
                    //var articleWithBody = client.nntp.Body(article.MessageId);
                    //var text = String.Join("\n", articleWithBody.Article.Body.ToList());

                    //if (text.StartsWith("beginn"))
                    //{
                    //    //uuenceded
                    //    try
                    //    {
                    //        var d = uuDecode(text);
                    //    }
                    //    catch (Exception exo)
                    //    {
                    //        this.Dispatcher.Invoke(() =>
                    //        {
                    //            ArticleBody.Text = text;
                    //        });

                    //    }
                    //}
                    //else
                    //{
                    //    this.Dispatcher.Invoke(() =>
                    //    {
                    //        ArticleBody.Text = text;
                    //    });
                    //}
                }
            }
            catch (Exception ex)
            {
            }
        }

        private async Task LoadNewsgroup()
        {
            var credentials = SettingsModel.GetCredentials();
            var settings = SettingsModel.Read();

            var client = await ConnectionManager.GetClient();

            var group = client.Group(SelectedNewsgroup);

            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();

            BindingOperations.EnableCollectionSynchronization(articles, _syncLock);
            articles.Clear();

            SelectedGroupHigh = group.Group.HighWaterMark;
            SelectedGroupLow = group.Group.LowWaterMark;

            Task.Factory.StartNew(() => GetFirst());
            
        }

        async Task GetFirst()
        {
            var credentials = SettingsModel.GetCredentials();
            var settings = SettingsModel.Read();
            var client = await ConnectionManager.GetClient();

            client.Group(SelectedNewsgroup);

            var e = 0;
            for (var i = SelectedGroupHigh; i != SelectedGroupLow; i--)
            {
                var a = client.Head(i);
                if (a.Code == 221)
                {
                    articles.Add(ArticleFactory.GetArticle(a));
                }

                e++;

                if (e > 20)
                {
                    SelectedGroupHigh = i - 1;
                    break;
                }
            }
        }

        protected void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            NewsgroupContentListBox.ItemsSource = articles;
        }

        private void DownloadButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _ = DownloadSelected();
        }

        private async Task DownloadSelected()
        {
            var settings = SettingsModel.Read();

            var client = await ConnectionManager.GetClient();
            var article = client.Article(SelectedArticle.MessageId);

            using (YencStream yencStream = YencStreamDecoder.Decode(article.Article.Body))
            {
                YencHeader header = yencStream.Header;

                VistaSaveFileDialog dialog = new VistaSaveFileDialog
                {
                    FileName = header.FileName
                };
                if (dialog.ShowDialog().Value)
                {
                    var target = dialog.FileName;

                    if (!File.Exists(target))
                    {
                        // create file and pre-allocate disk space for it
                        using (FileStream stream = File.Create(target))
                        {
                            stream.SetLength(header.FileSize);
                        }
                    }
                    using (FileStream stream = File.Open(
                        target, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                    {
                        // copy incoming parts to file
                        stream.Position = header.PartOffset;
                        yencStream.CopyTo(stream);
                    }
                }
            }
        }
    }
}
