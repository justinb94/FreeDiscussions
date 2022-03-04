using FreeDiscussions.Plugin;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FreeDiscussions.Plugins.Weather
{
    [System.ComponentModel.Composition.Export(typeof(IPlugin))]
    public class Plugin : FreeDiscussions.Plugin.Plugin
    {
        public override PanelType Type { get => PanelType.Main; set { } }
        public override string Guid { get => "FreeDiscussions.Plugins.Weather"; set { } }
        public override string Name { get => "Weather"; set { } }
        public override string IconPath { get => "/FreeDiscussions.Plugins.Weather;component/Resources/icon.svg"; set { } }

        public override async Task<TabItemModel> Create(params object[] args)
        {
            return new TabItemModel(this.Guid)
            {
                HeaderText = "Weather",
                Close = new DelegateCommand<string>((o) => { }),
                IconPath = this.IconPath,
                Control = new Panel(() =>
                {
                    //
                })
            };
        }
    }
}
