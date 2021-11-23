using FreeDiscussions.Client.Factory;
using FreeDiscussions.Client.Models;
using Ookii.Dialogs.Wpf;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
            BindingOperations.EnableCollectionSynchronization(articles, _syncLock);
            Task task = Task.Run(async () => await LoadNewsgroup());
        }

        private void NewsgroupContentListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;

            Task task = Task.Run(async () => await LoadMessage(e.AddedItems[0] as ArticleModel));
        }

        private async Task LoadMessage(ArticleModel article)
        {
            this.Dispatcher.Invoke(() =>
            {
                ArticleBody.Text = "Loading...";
                DownloadButton.Visibility = System.Windows.Visibility.Hidden;
            });


            SelectedArticle = article;

            var client = await ConnectionManager.GetClient();
            try
            {
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
                            ArticleContentRow.Height = new System.Windows.GridLength(280);
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
                                    DownloadButton.Visibility = System.Windows.Visibility.Visible;
                                    ArticleBody.Text = "This is a binary. Press Download to save on disk.";
                                    ArticleGridToolbarRow.Height = new System.Windows.GridLength(28);
                                    ArticleContentRow.Height = new System.Windows.GridLength(280);
                                    Splitter.Visibility = System.Windows.Visibility.Visible;
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
                            ArticleGridToolbarRow.Height = new System.Windows.GridLength(0);
                            ArticleContentRow.Height = new System.Windows.GridLength(280);
                            Splitter.Visibility = System.Windows.Visibility.Visible;
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
            finally
            {
                try
                {
                    client.Quit();
                }
                catch { }
            }
        }

        private async Task LoadNewsgroup()
        {
            Log.Information("LoadNewsgroup...");
            var client = await ConnectionManager.GetClient();
            Log.Information("Got a client.");
            try
            {
                Log.Information("Select group");
                var group = client.Group(SelectedNewsgroup);
                Log.Information("Group selected");

                Log.Information("Invoke creating a DispatchTimer");
                this.Dispatcher.Invoke(() =>
                {
                    Log.Information("Create DispatchTimer");
                    DispatcherTimer dispatcherTimer = new DispatcherTimer();
                    dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                    dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
                    dispatcherTimer.Start();
                    Log.Information("DispatchTimer started");
                });

                Log.Information("Clear articles");
                articles.Clear();

                SelectedGroupHigh = group.Group.HighWaterMark;
                SelectedGroupLow = group.Group.LowWaterMark;

                Log.Information($"HIGH: {SelectedGroupHigh}, LOW: {SelectedGroupLow}");

                Task task = Task.Run(() => GetFirst());
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Error LoadNewsgroup");

                var x = await ConnectionManager.CheckConnection();
                this.Dispatcher.Invoke(() =>
                {
                    if (!x)
                    {
                        UIManager.Instance.ClosePanel(SelectedNewsgroup);
                        MessageBox.Show("Can't connect. Please check your settings.");
                        UIManager.Instance.ShowSettingsPanel();
                    }
                });
            }
            finally
            {
                try
                {
                    client.Quit();
                } 
                catch
                {
                    // if connection failed, Quit will fail as well
                }
            }
        }

        async Task GetFirst()
        {
            Log.Information("GetFirst");

            var credentials = SettingsModel.GetCredentials();
            var settings = SettingsModel.Read();
            var client = await ConnectionManager.GetClient();
            try
            {

                var g = client.Group(SelectedNewsgroup);
                var e = 0;

                for (var i = SelectedGroupHigh; i != SelectedGroupLow; i--)
                {
                    var a = client.Head(i);
                    if (a.Code == 221)
                    {
                        Log.Information($"Received article {a.Article.MessageId}");
                        articles.Add(ArticleFactory.GetArticle(a));
                    } else
                    {
                        Log.Information($"Error receiving article, StatusCode: {a.Code}");
                    }

                    e++;

                    if (e > 100)
                    {
                        SelectedGroupHigh = i - 1;
                        break;
                    }
                }
            }
            finally
            {
                client.Quit();
            }
        }

        protected void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Log.Information("dispatcherTimer_Tick");

            NewsgroupContentListBox.ItemsSource = articles;
        }

        private void DownloadButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Task task = Task.Run(async () => await DownloadSelected());
        }

        private async Task DownloadSelected()
        {
            var settings = SettingsModel.Read();

            var client = await ConnectionManager.GetClient();
            try
            {
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
            finally
            {
                client.Quit();
            }
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            PostWindow w = new PostWindow(SelectedNewsgroup);
            w.ShowDialog();
        }
    }
}
