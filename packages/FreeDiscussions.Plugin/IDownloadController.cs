using System.Collections.Generic;
using System.Threading.Tasks;
using Usenet.Nntp.Models;
using Usenet.Nzb;

namespace FreeDiscussions.Plugin
{
    /// <summary>
    /// DownloadController interface
    /// </summary>
    public interface IDownloadController
    {
        /// <summary>
        /// Checks if the given article is a file
        /// </summary>
        /// <param name="articleId"></param>
        /// <returns></returns>
        Task<bool> IsFile(string articleId);

        /// <summary>
        /// Downloads the given message from the given newsgroup
        /// </summary>
        /// <param name="messageId">Message Id</param>
        /// <param name="newsgroup">Newsgroup Name</param>
        /// <returns></returns>
        Task DownloadArticle(string messageId, string newsgroup);

        /// <summary>
        /// Downloads the given NZB file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="folder"></param>
        void DownloadNzb(string fileName, string folder);

        /// <summary>
        /// Downloads the giben NZBFiles
        /// </summary>
        /// <param name="name">Download Name</param>
        /// <param name="folder">Folder path</param>
        /// <param name="files">List of NZBFIles</param>
        /// <param name="archivePassword">Password for extraction</param>
        void DownloadNzb(string name, string folder, List<NzbFile> files, ICollection<string> archivePassword);
    }

}

