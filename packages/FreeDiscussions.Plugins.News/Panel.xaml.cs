using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FreeDiscussions.Plugins.News
{
	/// <summary>
	/// Interaction logic for Panel.xaml
	/// </summary>
	public partial class Panel : FreeDiscussions.Plugin.Panel
	{
		public Panel(Action onClose) : base(onClose)
		{
			InitializeComponent();

			// https://developer.microsoft.com/de-de/microsoft-edge/webview2/#download-section

			//InitializeWebview();
		}

		private async Task InitializeWebview()
		{
			//try
			//{
			//    var env = await CoreWebView2Environment.CreateAsync(null, "C:\\Temp");
			//    await webView.EnsureCoreWebView2Async(env);
			//    webView.Source = new Uri("https://www.google.com");
			//}
			//catch (Exception e)
			//{
			//    MessageBox.Show(" Error in Web View " + e.Message, "Application");
			//}
		}
	}
}
