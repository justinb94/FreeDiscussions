using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
using System.Windows.Threading;

namespace FreeDiscussions.Plugins.Clock
{
	/// <summary>
	/// Interaction logic for Panel.xaml
	/// </summary>
	public partial class Panel : FreeDiscussions.Plugin.Panel
	{
		public Panel(Action onClose) : base(onClose)
		{
			InitializeComponent();

			DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);
			timer.Interval = TimeSpan.FromSeconds(1);
			timer.IsEnabled = true;
			timer.Tick += (s, e) =>
			{
				UpdateTime();
			};

			this.DataContext = new ViewModel();
			UpdateTime();
		}

		private void UpdateTime()
		{
			var all = TimeZoneInfo.GetSystemTimeZones().ToList();
			var vm = this.DataContext as ViewModel;

			vm.Items = all.Select(x => new Item { Name = x.DisplayName, Time = DateTime.UtcNow.AddHours(x.BaseUtcOffset.TotalHours) }).ToList();
			vm.Current = DateTime.Now.ToShortTimeString();
		}


		public class ViewModel : INotifyPropertyChanged
		{
			private List<Item> _items = new List<Item>();

			public List<Item> Items
			{
				get { 
					return _items; 
				}
				set { 
					_items = value; 
					this.OnPropertyChanged("Items"); 
				}
			}

			private string _current = DateTime.Now.ToShortTimeString();

			public string Current
			{
				get
				{
					return _current;
				}
				set
				{
					_current = value;
					this.OnPropertyChanged("Current");
				}
			}

			public event PropertyChangedEventHandler PropertyChanged;

			public void OnPropertyChanged(string propertyName)
			{
				if (PropertyChanged != null)
				{
					PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
				}
			}
		}

		public class Item : INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;
			public string Name { get; set; }
			private DateTime _time;

			public DateTime Time
			{
				get { return _time; }
				set { _time = value; this.OnPropertyChanged("Time"); }
			}

			public void OnPropertyChanged(string propertyName)
			{
				if (PropertyChanged != null)
				{
					PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
				}
			}
		}
	}
}
