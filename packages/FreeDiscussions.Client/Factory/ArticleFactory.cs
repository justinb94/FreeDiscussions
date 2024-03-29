﻿using FreeDiscussions.Plugin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Usenet.Nntp.Responses;

namespace FreeDiscussions.Client.Factory
{
    public static class ArticleFactory
    {
        public static ArticleModel GetArticle(NntpArticleResponse r)
        {
            ArticleModel result = new ArticleModel();
            result.Subject = r.Article.Headers.First(x => x.Key == "Subject").Value.First();

            DateTime d;
            if (DateTime.TryParse(r.Article.Headers.First(x => x.Key == "Date").Value.First(), out d))
            {
                result.Date = d;
            } else
            {
                Console.WriteLine(".");
            }
            result.MessageId = r.Article.MessageId;
            return result;
        }
    }
}
