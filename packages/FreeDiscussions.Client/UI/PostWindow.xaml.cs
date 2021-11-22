﻿using FreeDiscussions.Client.Models;
using System;
using System.Collections.Generic;
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

namespace FreeDiscussions.Client.UI
{
    /// <summary>
    /// Interaction logic for PostWindow.xaml
    /// </summary>
    public partial class PostWindow : Window
    {
        private readonly string settingsPath = System.IO.Path.Combine(Environment.CurrentDirectory, "settings.json");
        private string targetNewsgroup = "";

        public PostWindow(string targetNewsgroup)
        {
            InitializeComponent();
            // Name, E-Mail, Subject, Body
            this.targetNewsgroup = targetNewsgroup;
            this.Title = @$"Post to {targetNewsgroup}";

            this.DataContext = new NewMessageModel
            {
                Name = "My Name",
                Email = "my@email.com",
                Subject = "Subject",
                Body = "Body"
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

                var x = this.DataContext as NewMessageModel;

                NntpArticle newArticle = new NntpArticleBuilder()
                    .SetMessageId(messageId)
                    .SetFrom(@$"{x.Name} <{x.Email}>")
                    .SetSubject(x.Subject)
                    .AddGroups(this.targetNewsgroup)
                    .AddLines(x.Body.Split("/n"))
                    .Build();

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