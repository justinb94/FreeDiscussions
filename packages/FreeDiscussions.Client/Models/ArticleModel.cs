using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FreeDiscussions.Client.Models
{
    public class ArticleModel
    {
        public string Subject { get; set; }
        public string MessageId { get; internal set; }
    }
}
