using FreeDiscussions.Client.UI;

namespace FreeDiscussions
{
    public class Context
    {
        public static Context Instance;

        public IUIManager_ UIManager { get; set; }
    }
}
