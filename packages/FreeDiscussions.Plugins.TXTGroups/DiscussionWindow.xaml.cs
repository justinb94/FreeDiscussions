using FreeDiscussions.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Usenet.Nntp.Responses;

namespace FreeDiscussions.Plugins.TXTGroups
{
    /// <summary>
    /// Interaction logic for DiscussionWindow.xaml
    /// </summary>
    public partial class DiscussionWindow : Window
    {
        private List<NntpArticleResponse> Articles;
        private string Newsgroup;

        Func<String, TextBlock> HeadText = delegate (String text)
        {
            return new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = new SolidColorBrush(Colors.Blue),
                Margin = new Thickness(0, 4, 0, 0)
            };
        };

        Func<String, TextBlock> ContentText = delegate (String text)
        {
            return new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = new SolidColorBrush(Colors.Black),
                Margin = new Thickness(0, 4, 0, 0)
            };
        };

        public DiscussionWindow(List<NntpArticleResponse> articles, string newsgroup)
        {
            InitializeComponent();
            this.Articles = articles;
            this.Newsgroup = newsgroup;
            Load();
        }

        private async void Load()
        {

            //client.Group(this.Newsgroup);

            var content = await Task.WhenAll(this.Articles.Select(async x =>
            {
                NntpArticleResponse body = null;
                var client = await ConnectionManager.GetClient();
                try
                {
                    //client.Group(this.Newsgroup);
                    body = client.Body(x.Article.MessageId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);

                }
                finally
                {
                    try
                    {
                        client.Quit();
                    } catch { }
                }
                return new
                {
                    Article = x, 
                    Body = body
                };
            }).ToList());

            foreach (var article in content)
            {
                if (article.Body.Success)
                {
                    var text = string.Join("\n", article.Body.Article.Body);
                    var from = this.TruncString(article.Article.Article.Headers.First(x => x.Key == "From").Value.First(), 100);
                    var date = Rfc822Date.Parse(article.Article.Article.Headers.First(x => x.Key == "Date").Value.First());
                    var _from = from.Split("<");
                    if (_from.Length > 1)
                    {
                        from = _from[0].Trim();
                    }

                    this.ArticleContent.Children.Add(HeadText($"{from} am {date}"));
                    this.ArticleContent.Children.Add(ContentText(text));
                } else
                {
                    this.ArticleContent.Children.Add(ContentText("Artikel konnte nicht geladen werden"));
                }
                this.ArticleContent.Children.Add(new Separator
                {
                    Margin = new Thickness(0, 12, 0, 12)
                });
                this.ScrollViewer.InvalidateVisual();
            }


        }

        public string TruncString(string value, int maxLength = 50)
        {
            if (value.Length > maxLength)
                return value.Substring(0, maxLength) + "...";
            return value;
        }


    }
}
