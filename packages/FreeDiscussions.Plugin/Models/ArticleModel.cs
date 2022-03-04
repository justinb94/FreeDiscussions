using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FreeDiscussions.Plugin.Models
{
    public class ArticleModel
    {
        public string Subject { get; set; }
        public string MessageId { get; set; }
        public DateTime Date { get; set; }
    }
}
