using FreeDiscussions.Client.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Usenet.Nntp;

namespace FreeDiscussions.Client.UI
{
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class SettingsPanel : Panel
    {
        private readonly string settingsPath = System.IO.Path.Combine(Environment.CurrentDirectory, "settings.json");
        public SettingsModel settings = new SettingsModel { Hostname = "", Port = 119, SSL = false };
        public SecureString Password { get; set; }

        public SettingsPanel(Action onClose) : base(onClose)
        {
            InitializeComponent();
			if (File.Exists(settingsPath))
			{
				try
				{
					this.settings = SettingsModel.Read(settingsPath);

					var login = SettingsModel.GetCredentials();
					if (login != null)
					{
						this.UsernameTextBox.Text = login.Username;
						this.PasswordBox.Password = login.Password;
						this.Password = this.PasswordBox.SecurePassword;
					}
				}
				catch { }

				this.settings.Save(settingsPath);
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
				MessageBox.Show("Can't connect, please check your settings.");
				return;
			}

			SettingsModel.SetCredentials(this.UsernameTextBox.Text, new System.Net.NetworkCredential(string.Empty, this.Password).Password);
			string json = JsonConvert.SerializeObject(this.settings, Formatting.Indented);
			File.WriteAllText(settingsPath, json);
			this.onClose();
		}

		private void ChoseDownloadFolderButton_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
			if (dialog.ShowDialog(this.Parent as Window).GetValueOrDefault())
			{
				DownloadFolderTextBox.Text = dialog.SelectedPath;
				this.settings.DownloadFolder = dialog.SelectedPath;
			}
		}
	}

	public class ConnectionManager
	{
		private static List<NntpClient> _clients = new List<NntpClient>();

		public static async Task<bool> CheckConnection(string host, int port, string userName, string password, bool useSSL)
		{
			var client = new NntpClient(new NntpConnection());
			try
			{
				await client.ConnectAsync(host, port, useSSL);
				client.Authenticate(userName, password);
				client.Quit();
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
			return false;
		}
	}
}
