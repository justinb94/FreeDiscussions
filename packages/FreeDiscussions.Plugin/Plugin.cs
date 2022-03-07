using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace FreeDiscussions.Plugin
{
    public abstract class Plugin : IPlugin
    {
        public string Path { get; set; }
        public abstract PanelType Type { get; set; }
        public abstract string Guid { get; set; }
        public abstract string Name { get; set; }
        public abstract string IconPath { get; set; }

        public abstract Task<TabItemModel> Create(params object[] args);
    }
}