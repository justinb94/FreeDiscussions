using System.Threading.Tasks;
using Usenet.Nntp.Models;

namespace FreeDiscussions.Plugin
{
    public interface IDownloadController
    {
        Task<bool> IsFile(string articleId);
        void DownloadArticle(string messageId, string newsgroup);
    }

}

