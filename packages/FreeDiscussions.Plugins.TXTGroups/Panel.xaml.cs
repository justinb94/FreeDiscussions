using FreeDiscussions.Plugin;
using FreeDiscussions.Plugins.TXTGroups.DataGrid;
using Microsoft.Web.WebView2.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using Usenet.Nntp;
using Usenet.Nntp.Responses;
using Path = System.IO.Path;

namespace FreeDiscussions.Plugins.TXTGroups
{
    /// <summary>
    /// Interaction logic for Panel.xaml
    /// </summary>
    public partial class Panel : FreeDiscussions.Plugin.Panel
    {
        private Plugin plugin;
        long SelectedGroupHigh = 0;
        long SelectedGroupLow = 0;
        long Index = 0;
        long HeaderLoaded = 0;
        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        List<NntpArticleResponse> allHeaders = new List<NntpArticleResponse>();
        List<NntpArticleResponse> allBodies = new List<NntpArticleResponse>();

        public static class KnownFolder
        {
            public static readonly Guid Downloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out string pszPath);


        public Panel(Plugin plugin, Action onClose) : base(onClose)
        {
            InitializeComponent();

            this.plugin = plugin;
            this.grid.ItemsSource = new TreeGridModel().FlatModel;
            this.ProgressBar.Visibility = Visibility.Collapsed;
            this.HeaderState.Visibility = Visibility.Visible;
            this.HeaderState.Text = "Initialisierung...";

            Init();

           

            //var model = new TreeGridModel();

            //Item first = new Item() { Subject = "Ich kann euch was erzählen", From = "Denis Knaack", Date = DateTime.Now };
            //Item sub = new Item() { Subject = "RE: Ich kann ich was erählen", From = "Sebastian Gruber", Date = DateTime.Now };
            //first.Children.Add(sub);

            //sub.Children.Add(new Item() { Subject = "RE: Ich kann ich was erählen", From = "Denis Knaack", Date = DateTime.Now });
            //model.Add(first);


            //grid.ItemsSource = model.FlatModel;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //var model = grid.ItemsSource as TreeGridFlatModel;
            //model[0].Children[0].Children.Add(new Item() { Subject = "RE: Ich kann ich was erählen", From = "Denis Knaack", Date = DateTime.Now });

            //DoSomething();
        }

        private async void Init()
        {
            var client = await ConnectionManager.GetClient();
            var group = client.Group(this.plugin.Name);
            SelectedGroupHigh = group.Group.HighWaterMark;
            SelectedGroupLow = group.Group.LowWaterMark;
            Index = SelectedGroupHigh;
            client.Quit();

            Log.Information($"HIGH: {SelectedGroupHigh}, LOW: {SelectedGroupLow}");

            Task task = Task.Run(() => GetHeaders());
        }

        private async void GetHeaders(int count = 100)
        {
            Log.Information("Invoke creating a DispatchTimer");
            this.Dispatcher.Invoke(() =>
            {
                Log.Information("Create DispatchTimer");
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Interval = new TimeSpan(0, 0, 3);
                dispatcherTimer.Start();
                Log.Information("DispatchTimer started");

                this.LoadAllHeaders.IsEnabled = false;
                this.LoadNextHeaders.IsEnabled = false;


                this.HeaderState.Text = $"Header: {this.HeaderLoaded} / {this.SelectedGroupHigh - this.SelectedGroupLow}";
                this.HeaderState.Visibility = Visibility.Collapsed;
                this.ProgressBar.Maximum = this.SelectedGroupHigh - this.SelectedGroupLow;
                this.ProgressBar.Minimum = 0;
                this.ProgressBar.Value = this.HeaderLoaded;
                this.ProgressBar.Visibility = Visibility.Visible;
            });

            var client = await ConnectionManager.GetClient();
            var group = client.Group(this.plugin.Name);

            try
            {
                client.Group(this.plugin.Name);

                var e = 0;
                for (var i = this.Index; i != SelectedGroupLow; i--)
                {
                    this.Index--;
                    try
                    {
                        var articleHeader = client.Head(i);
                        this.HeaderLoaded++;

                        this.Dispatcher.Invoke(() =>
                        {
                            this.ProgressBar.Value = this.HeaderLoaded;
                        });


                        if (articleHeader.Code == 221)
                        {
                            Log.Information($"Received article {articleHeader.Article.MessageId}");
                            //
                            allHeaders.Add(articleHeader);
                        }
                        else
                        {
                            Log.Information($"Error receiving article, StatusCode: {articleHeader.Code}");
                        }

                        e++;

                        if (e > count)
                        {
                            break;
                        }
                    } catch { }
                }
            }
            finally
            {
                client.Quit();
                this.Dispatcher.Invoke(() =>
                {
                    this.dispatcherTimer.Stop();
                    this.dispatcherTimer_Tick(this, null);
                    Log.Information("DispatchTimer stopped");

                    this.ProgressBar.Visibility = Visibility.Collapsed;
                    this.HeaderState.Text = $"Header: {this.HeaderLoaded} / {this.SelectedGroupHigh - this.SelectedGroupLow}";
                    this.HeaderState.Visibility = Visibility.Visible;

                    this.LoadAllHeaders.IsEnabled = this.Index != this.SelectedGroupLow;
                    this.LoadNextHeaders.IsEnabled = this.Index != this.SelectedGroupLow;
                });
            }

            Console.WriteLine(allHeaders);
        }

        protected void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Log.Information("dispatcherTimer_Tick");
            this.dispatcherTimer.Stop();
            try
            {
                var treeItems = this.allHeaders.Select(a =>
                {
                    var from = this.TruncString(a.Article.Headers.First(x => x.Key == "From").Value.First(), 100);
                    var _from = from.Split("<");
                    if (_from.Length > 1)
                    {
                        from = _from[0].Trim();
                    }
                    return new Item
                    {
                        Subject = this.TruncString(a.Article.Headers.First(x => x.Key == "Subject").Value.First(), 100),
                        Date = Rfc822Date.Parse(a.Article.Headers.First(x => x.Key == "Date").Value.First()),
                        From = from,
                        Article = a,
                        IsRead = PostRead.IsRead(this.plugin.Name, a.Article.MessageId)
                    };
                }).ToList();

                var tree = treeItems.GenerateTree(x => x.Article.Article.MessageId, x =>
                {
                    var rf = this.GetReferences(x.Article);
                    if (rf != null) return rf.Last();
                    return null;
                });

                var model = (this.grid.ItemsSource as TreeGridFlatModel);
                var i = tree.Count();
                var ie = allHeaders.Count();
                var se = model.Count();

                foreach (var t in tree)
                {
                    var x = model.FirstOrDefault(x =>
                    {
                        var _x = x as Item;
                        return _x.Article.Article.MessageId == t.Article.Article.MessageId;
                    });
                    // Add only new articles to the view
                    if (x == default(Item))
                    {
                        model.Parent.Add(t);
                        this.allHeaders = this.allHeaders.Where(x => x.Article.MessageId != t.Article.Article.MessageId).ToList();
                    }

                }
                this.grid.ItemsSource = model.Parent.FlatModel;
            } finally
            {
                this.dispatcherTimer.Start();
            }
        }

        public string TruncString(string value, int maxLength = 50)
        {
            if (value.Length > maxLength)
                return value.Substring(0, maxLength) + "...";
            return value;
        }



        private List<string> GetReferences(NntpArticleResponse article)
        {
            var hasReference = article.Article.Headers.FirstOrDefault(x => x.Key == "References");
            if (hasReference.Key != null)
            {
                return new List<string>(hasReference.Value.First().Split(" "));
            }
            return null;
        }

        private void grid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var item = e.AddedCells[0].Item as Item;

            item.IsRead = true;
            PostRead.MarkAsRead(this.plugin.Name, item.Article.Article.MessageId);

            var downloadFilePath = GenerateFilePath(item.Article.Article.MessageId);
            var downloadFileExists = File.Exists(downloadFilePath);

            this.DownloadButton.Visibility = downloadFileExists ? Visibility.Collapsed : Visibility.Visible;
            this.OpenDownloadFile.Visibility = downloadFileExists ? Visibility.Visible : Visibility.Collapsed;

            Task task = Task.Run(async () =>
            {
                var client = await ConnectionManager.GetClient();
                var group = client.Group(this.plugin.Name);

                try {
                    var articleWithBody = client.Body(item.Article.Article.MessageId);
                    this.allBodies.Add(articleWithBody);
                    var text = String.Join("\n", articleWithBody.Article.Body.ToList());
                    this.Dispatcher.Invoke(() =>
                    {
                        ArticleBody.Text = text;
                        ArticleContentRow.Height = new GridLength(280);
                        Splitter.Visibility = Visibility.Visible;
                    });
                } catch {
                    this.Dispatcher.Invoke(() =>
                    {
                        ArticleBody.Text = "Fehler beim Laden des Artikels. Bitte probieren Sie es später noch einmal.";
                        ArticleContentRow.Height = new GridLength(280);
                        Splitter.Visibility = Visibility.Visible;
                    });
                } finally
                {
                    client.Quit();
                }
            });
        }

        private void LoadAllHeaders_Click(object sender, RoutedEventArgs e)
        {
            Task task = Task.Run(async () => {
                GetHeaders( int.MaxValue);
            });
        }

        private void LoadNextHeaders_Click(object sender, RoutedEventArgs e)
        {
            Task task = Task.Run(async () => {
                GetHeaders();
            });
        }

        private void PostMessage_Click(object sender, RoutedEventArgs e)
        {
            var win = new PostWindow(this.plugin.Name);
            win.ShowDialog();
        }

        private void ReplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.grid.SelectedCells.Count == 0) return;

            var selectedArticle = this.grid.SelectedCells[0].Item as Item;

            var win = new PostWindow(this.plugin.Name, selectedArticle, this.ArticleBody.Text);
            win.ShowDialog();
        }

        private string GenerateFilePath(string messageId)
        {
            string downloads;
            SHGetKnownFolderPath(KnownFolder.Downloads, 0, IntPtr.Zero, out downloads);

            var destinationFolder = Path.GetFullPath(downloads + "/FreeDiscussions/" + this.plugin.Name + "/");
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            var fileName = messageId;
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }

            var destinationFile = Path.GetFullPath(destinationFolder + fileName + ".txt");
            return destinationFile;
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.grid.SelectedCells.Count == 0) return;

            var selectedArticle = this.grid.SelectedCells[0].Item as Item;

            File.WriteAllText(GenerateFilePath(selectedArticle.Article.Article.MessageId), this.ArticleBody.Text);

            this.DownloadButton.Visibility = Visibility.Collapsed;
            this.OpenDownloadFile.Visibility = Visibility.Visible;
        }

        private void OpenDiscussionButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.grid.SelectedCells.Count == 0) return;
            var selectedArticle = this.grid.SelectedCells[0].Item as Item;
            if (!selectedArticle.Article.Success) return;

            List<NntpArticleResponse> discussion = new List<NntpArticleResponse>();
            GetDiscussion(selectedArticle, discussion);
            discussion.Reverse();

            var subject = selectedArticle.Article.Article.Headers.First(x => x.Key == "Subject").Value.First();

            var wnd = new DiscussionWindow(discussion, this.plugin.Name);
            wnd.Title = $"{this.plugin.Name}: {subject}";
            wnd.ShowDialog();
        }

        private void GetDiscussion(Item item, List<NntpArticleResponse> list)
        {
            list.Add(item.Article);
            if (item.Parent != null)
            {
                GetDiscussion(item.Parent as Item, list);
            }
        }

        private void OpenDownloadFile_Click(object sender, RoutedEventArgs e)
        {
            var selectedArticle = this.grid.SelectedCells[0].Item as Item;
            var downloadFilePath = GenerateFilePath(selectedArticle.Article.Article.MessageId);

            System.Diagnostics.Process.Start("explorer.exe", new FileInfo(downloadFilePath).DirectoryName);
        }
    }


    internal static class GenericHelpers
    {
        /// <summary>
        /// Generates tree of items from item list
        /// </summary>
        /// 
        /// <typeparam name="T">Type of item in collection</typeparam>
        /// <typeparam name="K">Type of parent_id</typeparam>
        /// 
        /// <param name="collection">Collection of items</param>
        /// <param name="id_selector">Function extracting item's id</param>
        /// <param name="parent_id_selector">Function extracting item's parent_id</param>
        /// <param name="root_id">Root element id</param>
        /// 
        /// <returns>Tree of items</returns>
        public static IEnumerable<Item> GenerateTree(
            this IEnumerable<Item> collection,
            Func<Item, string> id_selector,
            Func<Item, string> parent_id_selector,
            string root_id = default(string))
        {
            foreach (var c in collection.Where(c => EqualityComparer<string>.Default.Equals(parent_id_selector(c), root_id)))
            {
                var result = c;
                var children = collection.GenerateTree(id_selector, parent_id_selector, id_selector(c));
                foreach (var child in children)
                {
                    var _child = child as Item;
                    var x = result.Children.FirstOrDefault(x =>
                    {
                        var _x = x as Item;
                        return _x.Article.Article.MessageId == _child.Article.Article.MessageId;
                    });

                    if (x == default(Item))
                    {
                        result.Children.Add(child);
                    }
                }
                yield return result;
            }
        }
    }
}
