using FreeDiscussions.Client.Models;
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
            result.Date = DateTime.Parse(r.Article.Headers.First(x => x.Key == "Date").Value.First());
            result.MessageId = r.Article.MessageId;
            return result;
        }
    }
}
