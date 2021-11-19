﻿using FreeDiscussions.Client.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace FreeDiscussions.Client.UI
{
    public partial class MainWindow : Window, IUIManager
    {
        // import some functions for the custom window
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();


        public MainWindow()
        {
            InitializeComponent();

            // initalize UIManager Singleton
            new UIManager(this);

            // add home tab
            var s = new ObservableCollection<TabItemModel>();
            s.Add(new TabItemModel
            {
                HeaderText = "Home",
                HeaderImage = "/FreeDiscussions.Client;component/Resources/home.svg",
                Control = new NewsgroupsPanel(() =>
                {
                    s.RemoveAt(s.IndexOf(s.Where(x => x.HeaderText == "Home").FirstOrDefault()));
                }),
                Close = new DelegateCommand<string>((s) =>
                {
                    var _s = MainPanel.ItemsSource as ObservableCollection<TabItemModel>;
                    var newSource = _s.Where(x => x.HeaderText != "Home").ToArray();
                    MainPanel.ItemsSource = newSource;
                    MainPanel.SelectedIndex = _s.Count - 1;
                })
            });
            SidebarPanel.ItemsSource = s;

            MainPanel.ItemsSource = new ObservableCollection<TabItemModel>();
            BottomPanel.ItemsSource = new ObservableCollection<TabItemModel>();
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

        private void ShowSettingsPanel()
        {
            var s = MainPanel.ItemsSource as ObservableCollection<TabItemModel>;
            if (s == null)
            {
                s = new ObservableCollection<TabItemModel>();
            }

            // check if settings panel is already open
            var current = s.Where(x => x.HeaderText == "Settings").Select((x, i) => new { Index = i }).FirstOrDefault();
            if (current != null)
            {
                // select settings panel
                MainPanel.SelectedIndex = current.Index;
                return;
            }

            // settings panel is not already open => open it
            s.Add(new TabItemModel
            {
                HeaderText = "Settings",
                HeaderImage = "/FreeDiscussions.Client;component/Resources/gear.svg",
                Control = new SettingsPanel(() =>
                {
                    s.RemoveAt(s.IndexOf(s.Where(x => x.HeaderText == "Settings").FirstOrDefault()));
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
            s.Add(new TabItemModel
            {
                HeaderText = name,
                HeaderImage = "/FreeDiscussions.Client;component/Resources/globe.svg",
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
    }
}
