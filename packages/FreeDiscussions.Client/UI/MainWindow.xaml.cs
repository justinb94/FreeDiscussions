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
using Serilog;
using Microsoft.Extensions.Logging;
using FreeDiscussions.Plugin.Models;
using Usenet.Nntp.Models;
using System.IO;
using Usenet.Nntp;
using Usenet.Nzb;

namespace FreeDiscussions.Client.UI
{


    /// <summary>
    /// Interaction logic for MainWindow2.xaml
    /// </summary>

    public partial class MainWindow : Window, IUIController, IDownloadController
    {
        public static class KnownFolder
        {
            public static readonly Guid Downloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out string pszPath);



        public MainWindow()
        {
            AppContext.SetSwitch("Switch.System.Runtime.Serialization.SerializationGuard.AllowFileWrites", true);

            InitializeComponent();

            Context.UIController = this;
            Context.DownloadController = this;

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
                            Context.UIController.OpenPanel(Plugin.PanelType.Main, new Plugin.TabItemModel("Settings")
                            {
                                HeaderText = "Settings",
                                IconPath = "/FreeDiscussions.Client;component/Resources/gear.svg",
                                Control = new SettingsPanel(() => {
                                    Context.UIController.ClosePanel("Settings");
                                }),
                                Close = new DelegateCommand<string>((o) =>
                                {
                                    Context.UIController.ClosePanel("Settings");
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
                PluginMenuItems = new List<MainWindowViewModel.PluginMenuItemModel>()
            };

            // open home panel on start
            Context.UIController.OpenPanel(PanelType.Sidebar, new TabItemModel("Home")
            {
                HeaderText = "Home",
                IconPath = "/FreeDiscussions.Client;component/Resources/home.svg",
                Control = new NewsgroupsPanel(() => { }),
            });

            Task.Run(async () => await CheckConnectionOrShowSettingsPanel());
            Task.Run(async () => await LoadPluginMenuItems());
            //this.InitializePlugins();
        }

        private async Task CheckConnectionOrShowSettingsPanel()
        {
            var settings = SettingsModel.Read();
            var credentials = SettingsModel.GetCredentials();

            if (!await ConnectionManager.CheckConnection(settings.Hostname, settings.Port, credentials.Username, credentials.Password, settings.SSL))
            {
                this.Dispatcher.Invoke(() =>
                {
                    Context.UIController.OpenPanel(Plugin.PanelType.Main, new Plugin.TabItemModel("Settings")
                    {
                        HeaderText = "Settings",
                        IconPath = "/FreeDiscussions.Client;component/Resources/gear.svg",
                        Control = new SettingsPanel(() => {
                            Context.UIController.ClosePanel("Settings");
                        }),
                        Close = new DelegateCommand<string>((o) =>
                        {
                            Context.UIController.ClosePanel("Settings");
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

        public async Task OpenPanel(PanelType type, TabItemModel tab)
        {
            var vm = this.DataContext as MainWindowViewModel;
            if (type == PanelType.Main)
            {
                var currentIndex = vm.MainPanelTabs.Select((item, index) => new { Item = item, Index = index }).Where(x => x.Item.Id == tab.Id && x.Item.HeaderText == tab.HeaderText).FirstOrDefault();
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

        void IUIController.UpdatePlugins()
        {
            var vm = this.DataContext as MainWindowViewModel;
            if (vm == null) return;
            vm.OnPropertyChanged("PinnedPluginMenuItems");
            vm.OnPropertyChanged("PluginMenuItems");
        }

        public async Task ClosePanel(string headerText)
        {
            var vm = this.DataContext as MainWindowViewModel;
            var controls = new ObservableCollection<TabItemModel>(vm.MainPanelTabs.Where(x => x.HeaderText != headerText).ToList());
            vm.MainPanelTabs = controls;
        }

        private async Task<List<MainWindowViewModel.PluginMenuItemModel>> LoadPluginMenuItems()
        {
            var result = new List<MainWindowViewModel.PluginMenuItemModel>(await InitializePlugins());
            this.Dispatcher.Invoke(() =>
            {
                result.Add(
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

                            Context.UIController.OpenPanel(PanelType.Sidebar, new TabItemModel("Home")
                            {
                                HeaderText = "Home",
                                IconPath = "/FreeDiscussions.Client;component/Resources/home.svg",
                                Control = new NewsgroupsPanel(() => { }),
                            });
                        })
                    }
                );

                var vm = this.DataContext as MainWindowViewModel;
                vm.PluginMenuItems = result;
            });

            return result;
        }

        private async Task<List<MainWindowViewModel.PluginMenuItemModel>> InitializePlugins()
        {
            var result = new List<MainWindowViewModel.PluginMenuItemModel>();

            var manager = new PluginManager();
            manager.Setup();

            try
            {
                _ = Dispatcher.Invoke(async () =>
                {
                    foreach (var plugin in PluginContainer.Instance.Plugins)
                    {
                        await plugin.Init();

                        System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(plugin.GetType().Assembly.Location);
                        string version = fvi.FileVersion;

                        if (plugin.Type == PanelType.Main || plugin.Type == PanelType.Sidebar)
                        {
                            result.Add(new MainWindowViewModel.PluginMenuItemModel
                            {
                                Name = plugin.Name,
                                Plugin = plugin,
                                IsPinned = true,
                                Tooltip = $"{plugin.Name}\nVersion: {version}",
                                IconPath = plugin.IconPath,
                                Click = new DelegateCommand<object>(async (o) =>
                                {
                                    var me = this.DataContext as MainWindowViewModel;
                                    me.ContextMenuVisibility = Visibility.Hidden;
                                    me.PluginMenuVisibility = Visibility.Hidden;

                                    var c = await plugin.Create();
                                    c.IconPath = plugin.IconPath;
                                    c.Close = new DelegateCommand<string>((o) =>
                                    {
                                        Context.UIController.ClosePanel(c.HeaderText);
                                    });

                                    Context.UIController.OpenPanel(plugin.Type, c);
                                })
                            });
                        }
                        else if (plugin.Type == PanelType.NewsgroupView)
                        {
                            result.Add(new MainWindowViewModel.PluginMenuItemModel
                            {
                                Name = plugin.Name,
                                Plugin = plugin,
                                IsPinned = true,
                                Tooltip = $"{plugin.Name}\nVersion: {version}",
                                IconPath = plugin.IconPath,
                                Click = new DelegateCommand<object>(async (o) =>
                                {
                                    var me = this.DataContext as MainWindowViewModel;
                                    me.ContextMenuVisibility = Visibility.Hidden;
                                    me.PluginMenuVisibility = Visibility.Hidden;

                                    Context.UIController.OpenPanel(PanelType.Sidebar, new TabItemModel("Home")
                                    {
                                        HeaderText = "Home",
                                        IconPath = "/FreeDiscussions.Client;component/Resources/home.svg",
                                        Control = new NewsgroupsPanel(() => { }),
                                    });
                                })
                            });
                        }
                    }
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return result;
        }

        public async Task<bool> IsFile(string articleId)
        {
            using (var _client = await ConnectionManager.GetClient())
            {
                var body = _client.Nntp.Body(articleId);
                if (!body.Success)
                {
                    Console.WriteLine("WT");
                }

                if (!body.Success) return false;
                var firstLine = body.Article.Body.FirstOrDefault();
                var secondLine = body.Article.Body.FirstOrDefault();

                return !String.IsNullOrEmpty(firstLine) && firstLine.StartsWith("=ybegin") || !String.IsNullOrEmpty(secondLine) && secondLine.StartsWith("=ybegin");
            }        
        }

        public void ShowSettingsPanel()
        {
            Context.UIController.OpenPanel(Plugin.PanelType.Main, new Plugin.TabItemModel("Settings")
            {
                HeaderText = "Settings",
                IconPath = "/FreeDiscussions.Client;component/Resources/gear.svg",
                Control = new SettingsPanel(() => {
                    Context.UIController.ClosePanel("Settings");
                }),
                Close = new DelegateCommand<string>((o) =>
                {
                    Context.UIController.ClosePanel("Settings");
                })
            });
        }

        IDownloadController GetDownloadPlugin()
        {
            foreach (var plugin in PluginContainer.Instance.Plugins)
            {
                var p = plugin as IDownloadController;
                if (p != null) return p;
            }
            return null;
        }

        public async Task DownloadArticle(string messageId, string folder)
        {
            var downloadPlugin = this.GetDownloadPlugin();
            if (downloadPlugin != null)
            {
                downloadPlugin.DownloadArticle(messageId, folder);
                return;
            }

            new Task(async () =>
            {
                using (var client = await ConnectionManager.GetClient())
                {
                    var article = client.Nntp.Body(messageId);
                    if (!article.Success) return;
                    var text = String.Join("\n", article.Article.Body.ToList());
                    File.WriteAllText(GenerateFilePath(messageId, folder), text);
                }
            }).RunSynchronously();
        }

        private string GenerateFilePath(string messageId, string folder)
        {
            string downloads;
            SHGetKnownFolderPath(KnownFolder.Downloads, 0, IntPtr.Zero, out downloads);

            var destinationFolder = System.IO.Path.GetFullPath(downloads + "/FreeDiscussions/" + folder + "/");
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            var fileName = messageId;
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }

            var destinationFile = System.IO.Path.GetFullPath(destinationFolder + fileName + ".txt");
            return destinationFile;
        }

        public void DownloadNzb(string fileName, string folder)
        {
            var downloadPlugin = this.GetDownloadPlugin();
            if (downloadPlugin != null)
            {
                downloadPlugin.DownloadNzb(fileName, folder);
                return;
            }
            else MessageBox.Show("Downloading Nzb Files is not supported. Please consider getting a plugin.", "Not supported", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void DownloadNzb(string name, string folder, List<NzbFile> files)
        {
            var downloadPlugin = this.GetDownloadPlugin();
            if (downloadPlugin != null)
            {
                downloadPlugin.DownloadNzb(name, folder, files);
                return;
            }
            else MessageBox.Show("Downloading Nzb Files is not supported. Please consider getting a plugin.", "Not supported", MessageBoxButton.OK, MessageBoxImage.Information);
        }
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

        private List<PluginMenuItemModel> pluginMenuItems;
        public List<PluginMenuItemModel> PluginMenuItems
        {
            get { return this.pluginMenuItems; }
            set
            {
                this.pluginMenuItems = value;
                this.OnPropertyChanged("PluginMenuItems");
                this.OnPropertyChanged("PinnedPluginMenuItems");
            }
        }

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
            public string Tooltip { get; set; }
            public DelegateCommand<object> Click { get; set; }

            private bool _isPinned;
            public bool IsPinned
            {
                get {
                    return this._isPinned;
                }
                set {
                    this._isPinned = value;
                    Context.UIController.UpdatePlugins();
                }
            }
            public string IconPath { get; set; }
            public IPlugin Plugin { get; set; }
        }
    }
}
