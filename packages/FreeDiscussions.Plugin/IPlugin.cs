using System.Collections.Generic;
using System.Threading.Tasks;

namespace FreeDiscussions.Plugin
{
    public interface IPlugin
    {
        public PanelType Type{ get; set; }

        public string Guid { get; set; }

        public string Name { get; set; }

        public string IconPath { get; set; }

        public Task<TabItemModel> Create(params object[] args);

        string Path { get; set; }
    }
}
