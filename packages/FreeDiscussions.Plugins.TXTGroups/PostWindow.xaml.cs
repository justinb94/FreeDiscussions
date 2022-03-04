using FreeDiscussions.Plugin;
using FreeDiscussions.Plugin.Models;
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
using Usenet.Nntp;
using Usenet.Nntp.Builders;
using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;

namespace FreeDiscussions.Plugins.TXTGroups
{
    /// <summary>
    /// Interaction logic for PostWindow.xaml
    /// </summary>
    public partial class PostWindow : Window
    {
        private readonly string settingsPath = System.IO.Path.Combine(Environment.CurrentDirectory, "settings.json");
        private string targetNewsgroup = "";
        private string? replyTo;
        public Item? ParentItem { get; set; }

        public PostWindow(string targetNewsgroup, Item parent = default(Item), string parentBody = "")
        {
            InitializeComponent();
            // Name, E-Mail, Subject, Body
            this.targetNewsgroup = targetNewsgroup;
            this.Title = @$"Post to {targetNewsgroup}";
            this.ParentItem = parent;

            var subject = parent != default(Item) ? "RE: " + parent.Article.Article.Headers.First(x => x.Key == "Subject").Value.First() : "Subject";
            var body = String.IsNullOrEmpty(parentBody) ? "" : String.Join("\n", parentBody.Split("\n").Select(x => $"> {x}").ToList()) + "\n\n\n";
            this.SendButton.Content = parent != default(Item) ? "Antworten" : "Nachricht senden";

            this.DataContext = new NewMessageModel
            {
                Name = "My Name",
                Email = "my@email.com",
                Subject = subject,
                Body = body
            };

            ContentRendered += (sender, args) =>
            {
                BodyText.CaretIndex = BodyText.Text.Length;
                BodyText.ScrollToEnd(); // not necessary for single line texts
                BodyText.Focus();
            };
        }

        public PostWindow(string targetNewsgroup, string replyTo, string subject, string body)
        {
            InitializeComponent();
            // Name, E-Mail, Subject, Body
            this.targetNewsgroup = targetNewsgroup;
            this.replyTo = replyTo;
            this.Title = @$"Post to {targetNewsgroup}";

            this.DataContext = new NewMessageModel
            {
                Name = "My Name",
                Email = "my@email.com",
                Subject = subject,
                Body = body
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _ = Post();
        }

        private async Task Post()
        {
            var client = await ConnectionManager.GetClient();
            try
            {

                string messageId = $"{Guid.NewGuid()}@example.net";

                var refs = new List<string>();
                if (this.ParentItem != default(Item))
                {
                    var parentRefs = this.ParentItem.Article.Article.Headers.FirstOrDefault(x => x.Key == "References");
                    if (parentRefs.Key != null)
                    {
                        refs.AddRange(new List<string>(parentRefs.Value.First().Split(" ")));
                    }
                    refs.Add(this.ParentItem.Article.Article.MessageId);
                Console.WriteLine(refs);
                }


                var x = this.DataContext as NewMessageModel;

                var builder = new NntpArticleBuilder()
                    .SetMessageId(messageId)
                    .SetFrom(@$"{x.Name} <{x.Email}>")
                    .SetSubject(x.Subject)
                    .AddGroups(this.targetNewsgroup)
                    .AddLines(x.Body.Split("/n"));

                if (refs.Count != 0)
                {
                    builder = builder.AddHeader("References", String.Join(" ", refs));
                }

                if (replyTo != null)
                {
                    builder.AddHeader("Reply-To", this.replyTo);
                }

                var newArticle = builder.Build();

                var result = client.Post(newArticle);
                if (result)
                {
                    MessageBox.Show("Message sent successfully");
                    Close();
                }
                else
                {
                    MessageBox.Show("Error posting new article.");
                }
            }
            finally
            {
                client.Quit();
            }
        }
    }


}
