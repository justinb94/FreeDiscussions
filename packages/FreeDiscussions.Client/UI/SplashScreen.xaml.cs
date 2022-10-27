using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FreeDiscussions.Client.UI
{
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        string _statusText = "";

        public string StatusText { get { return _statusText; } set { _statusText = value; OnPropertyChanged("StatusText"); } }

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
            this.DataContext = new ViewModel
            {
                StatusText = "Starting..."
            };
        }

        public bool ConfigFileMissing()
        {
            var result = MessageBox.Show("Config File missing. Do you want me to create a new for you?", "Config File Missing", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                return true;
            } else
            {
                return false;
            }
        }

        public bool ConfigFileInvalid()
        {
            var result = MessageBox.Show("Config File invalid. Do you want me to create a new for you?", "Config File Invalid", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Offline()
        {
            MessageBox.Show("Cant check plugins. Are you offline?", "Offline", MessageBoxButton.OK);
        }

        public void CantGetLatestVersionFromUrl(PluginConfigurationData plugin)
        {
            MessageBox.Show($"Cant find plugin {plugin.Name}.", "Plugin", MessageBoxButton.OK);
        }

        public void UnknownPlugin(PluginConfigurationData plugin)
        {
            MessageBox.Show($"Cant find plugin {plugin.Name}.", "Plugin", MessageBoxButton.OK);
        }

        public void SetStatusText(string text)
        {
            var vm = (ViewModel)this.DataContext;
            vm.StatusText = text;
            this.DataContext = vm;
            Thread.Sleep(2000);
        }
    
    }
}
