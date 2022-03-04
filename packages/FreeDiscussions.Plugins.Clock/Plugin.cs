﻿using FreeDiscussions.Plugin;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FreeDiscussions.Plugins.Clock
{
	[System.ComponentModel.Composition.Export(typeof(IPlugin))]
	public class Plugin : FreeDiscussions.Plugin.Plugin
	{
		public override PanelType Type { get =>  PanelType.Sidebar; set { } }
		public override string Guid { get => "FreeDiscussions.Plugins.Clock"; set { } }
		public override string Name { get => "Clock"; set { } }
		public override string IconPath { get => "/FreeDiscussions.Plugins.Clock;component/Resources/icon.svg"; set { } }

		public override async Task<TabItemModel> Create(params object[] args)
		{
			return new TabItemModel(this.Guid)
			{
				HeaderText = "Clock",
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
