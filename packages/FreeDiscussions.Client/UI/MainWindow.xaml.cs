using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Linq;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FreeDiscussions.Plugin;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using FreeDiscussions.Client.Models;

namespace FreeDiscussions.Client.UI
{


    /// <summary>
    /// Interaction logic for MainWindow2.xaml
    /// </summary>

    public partial class MainWindow : Window, IUIManager_
    {
        // import some functions for the custom window
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public MainWindow()
        {
            InitializeComponent();

            Context.Instance = new Context
            {
                UIManager = this
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

            // TODO: load plugins

            // create and bind view model
            this.DataContext = new MainWindowViewModel
            {
                CloseButtonClick = new DelegateCommand<object>((o) =>
                {
                    this.Close();
                }),
                MaximizeButtonClick = new DelegateCommand<object>((o) =>
                {
                    this.WindowState = this.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
                }),
                MinimizeButtonClick = new DelegateCommand<object>((o) =>
                {
                    this.WindowState = WindowState.Minimized;
                }),
                GithubButtonClick = new DelegateCommand<object>((o) =>
                {
                    var uri = "https://github.com/justinb94/FreeDiscussions";
                    var psi = new System.Diagnostics.ProcessStartInfo();
                    psi.UseShellExecute = true;
                    psi.FileName = uri;
                    System.Diagnostics.Process.Start(psi);
                }),
                WindowDrag = new DelegateCommand<object>((o) =>
                {
                    ReleaseCapture();
                    SendMessage(new WindowInteropHelper(this).Handle, 0xA1, (IntPtr)0x2, (IntPtr)0);
                }),
                DotsClicked = new DelegateCommand<object>((sender) =>
                {
                    var me = this.DataContext as MainWindowViewModel;
                    me.ContextMenuVisibility = me.ContextMenuVisibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                    me.PluginMenuVisibility = Visibility.Hidden;
                }),
                HideContextMenus = new DelegateCommand<object>((sender) =>
                {
                    var me = this.DataContext as MainWindowViewModel;
                    me.ContextMenuVisibility = Visibility.Hidden;
                    me.PluginMenuVisibility = Visibility.Hidden;
                }),
                PuzzleClicked = new DelegateCommand<object>((sender) =>
                {
                    var me = this.DataContext as MainWindowViewModel;
                    me.PluginMenuVisibility = me.PluginMenuVisibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                    me.ContextMenuVisibility = Visibility.Hidden;
                }),
                HidePluginMenu = new DelegateCommand<object>((sender) =>
                {
                    var me = this.DataContext as MainWindowViewModel;
                    me.PluginMenuVisibility = Visibility.Hidden;
                }),
                ContextMenuItems = new List<MainWindowViewModel.ContextMenuItemModel>
                {
                    new MainWindowViewModel.ContextMenuItemModel
                    {
                        Name = "Settings...",
                        Click = new DelegateCommand<object>((o) =>
                        {
                            Context.Instance.UIManager.OpenPanel(Plugin.PanelLocation.Main, new Plugin.TabItemModel("Settings")
                            {
                                HeaderText = "Settings",
                                IconPath = "/FreeDiscussions.Client;component/Resources/gear.svg",
                                Control = new SettingsPanel(() => {
                                    Context.Instance.UIManager.ClosePanel("Settings");
                                }),
                                Close = new DelegateCommand<string>((o) =>
                                {
                                    Context.Instance.UIManager.ClosePanel("Settings");
                                })
                            });
                        })
                    },
                    new MainWindowViewModel.ContextMenuItemModel
                    {
                        Name = "About...",
                        Click = new DelegateCommand<object>((o) =>
                        {
                            // TODO: open about window
                            var me = this.DataContext as MainWindowViewModel;
                            me.ContextMenuVisibility = Visibility.Hidden;
                        })
                    },
                    new MainWindowViewModel.ContextMenuItemModel
                    {
                        Name = "Close",
                        Click = new DelegateCommand<object>((o) =>
                        {
                            this.Close();
                        }),
                    }
                },
                PluginMenuItems = new List<MainWindowViewModel.PluginMenuItemModel>
                {
                    new MainWindowViewModel.PluginMenuItemModel
                    {
                        Name = "Home",
                        IsPinned = true,
                        IconPath = "/FreeDiscussions.Client;component/Resources/home.svg",
                        Click = new DelegateCommand<object>((o) =>
                        {
                            var me = this.DataContext as MainWindowViewModel;
                            me.ContextMenuVisibility = Visibility.Hidden;
                            me.PluginMenuVisibility = Visibility.Hidden;

                            Context.Instance.UIManager.OpenPanel(PanelLocation.Sidebar, new TabItemModel("Home")
                            {
                                HeaderText = "Home",
                                IconPath = "/FreeDiscussions.Client;component/Resources/home.svg",
                                Control = new NewsgroupsPanel(() => { }),
                            });
                        })
                    },
                },
                PinPluginClicked = new DelegateCommand<object>((o) =>
                {
                    Console.WriteLine(o);
                }),
                ContextMenuVisibility = Visibility.Hidden,
                PluginMenuVisibility = Visibility.Hidden,
                HideSidebar = new DelegateCommand<object>((o) =>
                {
                    this.HideSidebar();
                }),
                ShowSidebar = new DelegateCommand<object>((o) =>
                {
                    this.ShowSidebar();
                }),
            };

            // open home panel on start
            Context.Instance.UIManager.OpenPanel(PanelLocation.Sidebar, new TabItemModel("Home")
            {
                HeaderText = "Home",
                IconPath = "/FreeDiscussions.Client;component/Resources/home.svg",
                Control = new NewsgroupsPanel(() => { }),
            });

            Task.Run(async () => await CheckConnectionOrShowSettingsPanel());
        }

        private async Task CheckConnectionOrShowSettingsPanel()
        {
            var settings = SettingsModel.Read();
            var credentials = SettingsModel.GetCredentials();

            if (!await ConnectionManager.CheckConnection(settings.Hostname, settings.Port, credentials.Username, credentials.Password, settings.SSL))
            {
                this.Dispatcher.Invoke(() =>
                {
                    Context.Instance.UIManager.OpenPanel(Plugin.PanelLocation.Main, new Plugin.TabItemModel("Settings")
                    {
                        HeaderText = "Settings",
                        IconPath = "/FreeDiscussions.Client;component/Resources/gear.svg",
                        Control = new SettingsPanel(() => {
                            Context.Instance.UIManager.ClosePanel("Settings");
                        }),
                        Close = new DelegateCommand<string>((o) =>
                        {
                            Context.Instance.UIManager.ClosePanel("Settings");
                        })
                    });
                });
            }
        }

        public void HideSidebar()
        {
            var vm = this.DataContext as MainWindowViewModel;
            vm.SidebarVisibility = Visibility.Hidden;
        }

        public void ShowSidebar()
        {
            var vm = this.DataContext as MainWindowViewModel;
            vm.SidebarVisibility = Visibility.Visible;
        }

        public async Task OpenPanel(PanelLocation location, TabItemModel tab)
        {
            var vm = this.DataContext as MainWindowViewModel;
            if (location == PanelLocation.Main)
            {
                var currentIndex = vm.MainPanelTabs.Select((item, index) => new { Item = item, Index = index }).Where(x => x.Item.Id == tab.Id).FirstOrDefault();
                if (currentIndex != null) {
                    // tab already open
                    vm.SelectedIndexMainPanel = currentIndex.Index;
                    vm.OnPropertyChanged("MainPanelTabs");
                    return;
                }
                vm.MainPanelTabs.Add(tab);
                vm.SelectedIndexMainPanel = vm.MainPanelTabs.Count - 1;
                vm.OnPropertyChanged("MainPanelTabs");
            } else
            {
                var toggle = false;
                if (vm.SidebarItem != null && vm.SidebarItem.HeaderText == tab.HeaderText)
                {
                    toggle = true;
                }

                if (toggle)
                {
                    if (vm.SidebarVisibility == Visibility.Visible)
                    {
                        HideSidebar();
                    }
                    else
                    {
                        ShowSidebar();
                    }
                    return;
                }

                // open
                vm.SidebarVisibility = Visibility.Visible;
                vm.SidebarItem = tab;
                vm.OnPropertyChanged("SidebarItem");
                this.SidebarControlContainer.Child = tab.Control;
                this.ShowSidebar();
            }
        }

        void IUIManager_.UpdatePlugins()
        {
            var vm = this.DataContext as MainWindowViewModel;
            if (vm == null) return;
            vm.OnPropertyChanged("PinnedPluginMenuItems");
            vm.OnPropertyChanged("PluginMenuItems");
        }

        public async Task ClosePanel(string id)
        {
            var vm = this.DataContext as MainWindowViewModel;
            var controls = new ObservableCollection<TabItemModel>(vm.MainPanelTabs.Where(x => x.Id != id).ToList());
            vm.MainPanelTabs = controls;
        }
    }

    public interface IUIManager_
    {
        Task OpenPanel(PanelLocation location, TabItemModel tab);
        Task ClosePanel(string id);
        void HideSidebar();
        void ShowSidebar();
        void UpdatePlugins();
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public DelegateCommand<object> CloseButtonClick { get; set; }
        public DelegateCommand<object> MaximizeButtonClick { get; set; }
        public DelegateCommand<object> MinimizeButtonClick { get; set; }
        public DelegateCommand<object> WindowDrag{ get; set; }
        public DelegateCommand<object> DotsClicked { get; set; }
        public DelegateCommand<object> GithubButtonClick { get; set; }
        public DelegateCommand<object> HideContextMenus { get; set; }
        public DelegateCommand<object> PuzzleClicked { get; set; }
        public List<ContextMenuItemModel> ContextMenuItems { get; set; }
        public DelegateCommand<object> PinPluginClicked { get; set; }
        public TabItemModel SidebarItem { get; set; }

        private ObservableCollection<TabItemModel> _mainPanelTabs = new ObservableCollection<TabItemModel>();

        private int _selectedIndexMainPanel = 0;
        public int SelectedIndexMainPanel
        {
            get
            {
                return _selectedIndexMainPanel;
            }

            set
            {
                _selectedIndexMainPanel = value;
                OnPropertyChanged("SelectedIndexMainPanel");
            }
        }

        public ObservableCollection<TabItemModel> MainPanelTabs
    {
            get
            {
                return _mainPanelTabs;
            }

            set
            {
                _mainPanelTabs = value;
                OnPropertyChanged("MainPanelTabs");
            }
        }

        public DelegateCommand<object> HidePluginMenu { get; set; }
        public List<PluginMenuItemModel> PluginMenuItems { get; set; }
        public DelegateCommand<object> HideSidebar { get; set; }
        public DelegateCommand<object> ShowSidebar { get; set; }

        public List<PluginMenuItemModel> PinnedPluginMenuItems
        {
            get
            {
                return this.PluginMenuItems.Where(x => x.IsPinned).ToList();
            }
        }

        private Visibility _pluginMenuVisibility;
        public Visibility PluginMenuVisibility
        {
            get
            {
                return _pluginMenuVisibility;
            }

            set
            {
                _pluginMenuVisibility = value;
                OnPropertyChanged("PluginMenuVisibility");
            }
        }

        private Visibility _sidebarVisibility = Visibility.Visible;
        public Visibility SidebarVisibility
        {
            get
            {
                return this._sidebarVisibility;
            }
            set
            {
                this._sidebarVisibility = value;
                this.OnPropertyChanged("SidebarVisibility");
            }
        }

        private Visibility _contextMenuVisibility;
        public Visibility ContextMenuVisibility
        {
            get
            {
                return _contextMenuVisibility;
            }

            set
            {
                _contextMenuVisibility = value;
                OnPropertyChanged("ContextMenuVisibility");
            }
        }

        public MainWindowViewModel()
        {
            this.MainPanelTabs = new ObservableCollection<TabItemModel>();
            this.SidebarVisibility = Visibility.Visible;
        }

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public class ContextMenuItemModel
        {
            public string Name { get; set; }
            public DelegateCommand<object> Click { get; set; }
        }

        public class PluginMenuItemModel
        {
            public string Name { get; set; }
            public DelegateCommand<object> Click { get; set; }

            private bool _isPinned;
            public bool IsPinned
            {
                get {
                    return this._isPinned;
                }
                set {
                    this._isPinned = value;
                    Context.Instance.UIManager.UpdatePlugins();
                }
            }
            public string IconPath { get; set; }
            public IPlugin Plugin { get; set; }
        }
    }
}
