using FreeDiscussions.Client.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FreeDiscussions.Client.UI
{
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class SettingsPanel : Plugin.Panel
    {
        public SettingsModel settings = new SettingsModel { Hostname = "", Port = 119, SSL = false };
        public SecureString Password { get; set; }

        public SettingsPanel(Action onClose) : base(onClose)
        {
            InitializeComponent();
			if (File.Exists(SettingsModel.SettingsPath))
			{
				try
				{
					this.settings = SettingsModel.Read();

					var login = SettingsModel.GetCredentials();
					if (login != null)
					{
						this.UsernameTextBox.Text = login.Username;
						this.PasswordBox.Password = login.Password;
						this.Password = this.PasswordBox.SecurePassword;
					}
				}
				catch { }

				this.settings.Save();
			}

			this.DataContext = settings;
		}

		private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			if (DataContext != null)
			{ this.Password = ((PasswordBox)sender).SecurePassword; }
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			_ = SaveSettings();
		}

		private async Task SaveSettings()
		{
			if (!await ConnectionManager.CheckConnection(this.settings.Hostname, this.settings.Port, this.UsernameTextBox.Text, new System.Net.NetworkCredential(string.Empty, this.Password).Password, this.settings.SSL))
			{
				MessageBox.Show("Can't connect. Please check your settings.");
				return;
			}

			SettingsModel.SetCredentials(this.UsernameTextBox.Text, new System.Net.NetworkCredential(string.Empty, this.Password).Password);
			string json = JsonConvert.SerializeObject(this.settings, Formatting.Indented);
			File.WriteAllText(SettingsModel.SettingsPath, json);
			this.onClose();
		}

		//private void ChoseDownloadFolderButton_Click(object sender, RoutedEventArgs e)
		//{
		//	var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
		//	if (dialog.ShowDialog(this.Parent as Window).GetValueOrDefault())
		//	{
		//		DownloadFolderTextBox.Text = dialog.SelectedPath;
		//		this.settings.DownloadFolder = dialog.SelectedPath;
		//	}
		//}
	}
}
