using FreeDiscussions.Plugin;
using System.Threading.Tasks;

namespace HelloWorldPlugin
{
	[System.ComponentModel.Composition.Export(typeof(IPlugin))]
	public class Plugin : FreeDiscussions.Plugin.BasePlugin
	{
		public override PanelType Type { get => PanelType.Sidebar; set { } }
		public override string Guid { get => "HelloWorldPlugin"; set { } }
		public override string Name { get => "HelloWorld"; set { } }
		public override string IconPath { get => "/HelloWorldPlugin;component/Resources/icon.svg"; set { } }

		public override async Task<TabItemModel> Create(params object[] args)
		{
			return new TabItemModel(this.Guid)
			{
				HeaderText = "Hello World",
				Close = new DelegateCommand<string>((o) => { }),
				IconPath = this.IconPath,
				Control = new Panel(() =>
				{
					// do something when the user closes the tab 
				})
			};
		}
	}
}
