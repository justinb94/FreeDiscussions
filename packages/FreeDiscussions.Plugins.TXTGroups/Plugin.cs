using FreeDiscussions.Plugin;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FreeDiscussions.Plugins.TXTGroups
{
	[System.ComponentModel.Composition.Export(typeof(IPlugin))]
	public class Plugin : FreeDiscussions.Plugin.Plugin
	{
		public override PanelType Type { get => PanelType.NewsgroupView; set { } }
		public override string Guid { get => "FreeDiscussions.Plugins.TXTGroups"; set { } }
		public override string Name { get; set; }
		public override string IconPath { get => "/FreeDiscussions.Plugins.TXTGroups;component/Resources/icon.svg"; set { } }

		Plugin()
		{
			this.Name = "Newsgroups Pro";
		}

		public override async Task<TabItemModel> Create(params object[] args)
		{
			if (args.Length > 0)
			{
				this.Name = args[0].ToString();	
			}
			return new TabItemModel(this.Guid)
			{
				HeaderText = this.Name,
				Close = new DelegateCommand<string>((o) => { }),
				IconPath = this.IconPath,
				Control = new Panel(this, () =>
				{
			//
				})			
			};
		}
	}
}
