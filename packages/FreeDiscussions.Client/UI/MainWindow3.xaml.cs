using FreeDiscussions.Client.Models;
using FreeDiscussions.Plugin;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace FreeDiscussions.Client.UI
{
    public partial class MainWindow3 : Window, IUIManager
    {
        // import some functions for the custom window
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        Action<NavigationItemModel> SelectedNavItemChanged;


        public MainWindow3()
        {
            InitializeComponent();

            this.SelectedNavItemChanged = async (NavigationItemModel me) =>
            {
                if (me.Plugin == null)
                {
                    // Home Panel
                    SidebarPanelColumn.Width = new GridLength(340);
                    SidebarGridSplitter.IsEnabled = true;
                } else {
                    var panel = me.Plugin;
                    if (panel.Location != PanelLocation.Sidebar)
                    {
                        SidebarPanelColumn.Width = new GridLength(0);
                        SidebarGridSplitter.IsEnabled = false;

                        var tabItem = await me.Plugin.Create();
                        var s = MainPanel.ItemsSource as ObservableCollection<TabItemModel>;
                        // TODO: open only one
                        s.Add(tabItem);
                        MainPanel.ItemsSource = s;
                        MainPanel.SelectedIndex = s.Count - 1;
                    }
                }
            };

            // initialize logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("log_.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var factory = new LoggerFactory();
            factory.AddSerilog();
            Usenet.Logger.Factory = factory;

            // initalize UIManager Singleton
            UIManager.Instance = this;

            // add home tab
            //var s = new ObservableCollection<TabItemModel>();
            //s.Add(new TabItemModel
            //{
            //    HeaderText = "Home",
            //    HeaderImage = "/FreeDiscussions.Client;component/Resources/home.svg",
            //    Control = new NewsgroupsPanel(() =>
            //    {
            //        s.RemoveAt(s.IndexOf(s.Where(x => x.HeaderText == "Home").FirstOrDefault()));
            //    }),
            //    Close = new DelegateCommand<string>((s) =>
            //    {
            //        var _s = MainPanel.ItemsSource as ObservableCollection<TabItemModel>;
            //        var newSource = _s.Where(x => x.HeaderText != "Home").ToArray();
            //        MainPanel.ItemsSource = newSource;
            //        MainPanel.SelectedIndex = _s.Count - 1;
            //    })
            //});
            //SidebarPanel.ItemsSource = s;
            SidebarPanel.Child = new NewsgroupsPanel(() => { });

            Navigation.ItemsSource = new ObservableCollection<NavigationItemModel>
            {
                new NavigationItemModel
                {
                    Name = "Home",
                    Selected = SelectedNavItemChanged
                }
            };

            MainPanel.ItemsSource = new ObservableCollection<TabItemModel>();
            // BottomPanel.ItemsSource = new ObservableCollection<TabItemModel>();

            Task.Run(async () => await CheckConnectionOrShowSettingsPanel());
            Task.Run(async () => await InitializePlugins());
        }

        private void Navigation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            var item = e.AddedItems[0] as NavigationItemModel;
            item.Selected(item);
        }

        public class NavigationItemModel
        {
            public string Name { get; set; }
            public Action<NavigationItemModel> Selected { get; set; }
            public IPlugin Plugin { get; set; }
        }

        private async Task InitializePlugins()
        {
            var manager = new PluginManager();
            manager.Setup();

            try
            {
                var s = Navigation.ItemsSource as ObservableCollection<NavigationItemModel>;

                _ = Dispatcher.Invoke(async () =>
                {
                    foreach (var plugin in PluginContainer.Instance.Plugins)
                    {
                        s.Add(new NavigationItemModel
                        {
                            Name = plugin.Name,
                            Selected = SelectedNavItemChanged,
                            Plugin = plugin
                        });   
                    }
                    Navigation.ItemsSource = s;
                });

            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task CheckConnectionOrShowSettingsPanel()
        {
            Log.Information("CheckConnectionOrShowSettingsPanel...");
            var settings = SettingsModel.Read();
            var credentials = SettingsModel.GetCredentials();

            if (!await ConnectionManager.CheckConnection(settings.Hostname, settings.Port, credentials.Username, credentials.Password, settings.SSL))
            {
                this.Dispatcher.Invoke(() =>
                {
                    ShowSettingsPanel();
                });
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void GithubButton_Click(object sender, RoutedEventArgs e)
        {
            var uri = "https://github.com/justinb94/FreeDiscussions";
            var psi = new System.Diagnostics.ProcessStartInfo();
            psi.UseShellExecute = true;
            psi.FileName = uri;
            System.Diagnostics.Process.Start(psi);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsPanel();
        }

        private void WindowDrag(object sender, MouseButtonEventArgs e) // MouseDown
        {
            ReleaseCapture();
            SendMessage(new WindowInteropHelper(this).Handle,
                0xA1, (IntPtr)0x2, (IntPtr)0);
        }

        private void WindowResize(object sender, MouseButtonEventArgs e)
        {
            HwndSource hwndSource = PresentationSource.FromVisual((Visual)sender) as HwndSource;
            SendMessage(hwndSource.Handle, 0x112, (IntPtr)61448, IntPtr.Zero);
        }

        public void ShowSettingsPanel()
        {
            var s = MainPanel.ItemsSource as ObservableCollection<TabItemModel>;
            if (s == null)
            {
                s = new ObservableCollection<TabItemModel>();
            }

            // check if settings panel is already open
            var current = s.Select((x, i) => new { Index = i, Item= x }).Where(x => x.Item.HeaderText == "Settings").FirstOrDefault();
            if (current != null)
            {
                // select settings panel
                MainPanel.SelectedIndex = current.Index;
                return;
            }

            // settings panel is not already open => open it
            s.Add(new TabItemModel("Settings")
            {
                HeaderText = "Settings",
                IconPath = "/FreeDiscussions.Client;component/Resources/gear.svg",
                Control = new SettingsPanel(() =>
                {
                    Context.Instance.UIManager.ClosePanel("Settings");
                }),
                Close = new DelegateCommand<string>((n) =>
                {
                    s.RemoveAt(s.IndexOf(s.Where(x => x.HeaderText == "Settings").FirstOrDefault()));
                })
            });

            MainPanel.ItemsSource = s;
            MainPanel.SelectedIndex = s.Count - 1;
        }

        public void OpenOrSelectNewsgroup(string name)
        {
            var s = MainPanel.ItemsSource as ObservableCollection<TabItemModel>;
            if (s == null)
            {
                s = new ObservableCollection<TabItemModel>();
            }

            // check if panel already exists
            if (s.Any(x => x.HeaderText == name))
            {
                var i = s.Select((x, i) => new { Item = x, Index = i }).Where(x => x.Item.HeaderText == name).First();
                MainPanel.SelectedIndex = i.Index;
                return;
            }

            // add panel
            s.Add(new TabItemModel(name)
            {
                HeaderText = name,
                IconPath = "/FreeDiscussions.Client;component/Resources/globe.svg",
                Control = new NewsgroupsContentPanel(name, () =>
                {
                    s.RemoveAt(s.IndexOf(s.Where(x => x.HeaderText == name).FirstOrDefault()));
                }),
                Close = new DelegateCommand<string>((n) =>
                {
                    s.RemoveAt(s.IndexOf(s.Where(x => x.HeaderText == name).FirstOrDefault()));
                })
            });
            MainPanel.ItemsSource = s;
            MainPanel.SelectedIndex = s.Count - 1;
        }

        public void ClosePanel(string name)
        {
            var items = MainPanel.ItemsSource as ObservableCollection<TabItemModel>;
            var panel = items.IndexOf(items.Where(x => x.HeaderText == name).FirstOrDefault());
            if (panel != -1) { 
                items.RemoveAt(panel);
            }
        }


    }
}
