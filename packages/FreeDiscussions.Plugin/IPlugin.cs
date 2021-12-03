using System.Collections.Generic;
using System.Threading.Tasks;

namespace FreeDiscussions.Plugin
{
    public interface IPlugin
    {
        public PanelLocation Location { get; set; }

        public string Guid { get; set; }

        public string Name { get; set; }

        public string IconPath { get; set; }

        public Task<TabItemModel> Create();

        string Path { get; set; }
    }
}
