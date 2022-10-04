namespace FreeDiscussions.Plugin
{
    /// <summary>
    /// Context to the client
    /// </summary>
    public class Context
    {
        /// <summary>
        /// Gets the DownloadController
        /// </summary>
        public static IDownloadController DownloadController { get; set; }

        /// <summary>
        /// Gets the UIController
        /// </summary>
        public static IUIController UIController { get; set; }
    }
}

