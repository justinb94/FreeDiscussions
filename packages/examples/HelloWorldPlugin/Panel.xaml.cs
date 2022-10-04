using System;

namespace HelloWorldPlugin
{
	/// <summary>
	/// Interaction logic for Panel.xaml
	/// </summary>
	public partial class Panel : FreeDiscussions.Plugin.Panel
	{
		public Panel(Action onClose) : base(onClose)
		{
			InitializeComponent();

		}
	}
}
