using FreeDiscussions.Plugin.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Usenet.Nntp;

namespace FreeDiscussions.Plugin
{
    public class ConnectionManager
	{
		private static List<NntpClient> _clients = new List<NntpClient>();

		public static async Task<bool> CheckConnection(string host, int port, string userName, string password, bool useSSL)
		{
			Log.Information("CheckConnection...");

			var client = new NntpClient(new NntpConnection());
			try
			{
				await client.ConnectAsync(host, port, useSSL);
				client.Authenticate(userName, password);
				client.Quit();

				Log.Information("CheckConnection succesful...");
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				Log.Error(ex, "CheckConnection failed...");
			}
			return false;
		}

		public static Task<bool> CheckConnection()
		{
			Log.Information("CheckConnection...");
			var settings = SettingsModel.Read();
			var login = SettingsModel.GetCredentials();
			return CheckConnection(settings.Hostname, settings.Port, login.Username, login.Password, settings.SSL);
		}

		public static async Task<NntpClient> GetClient()
		{
			Log.Information("GetClient");

			var settings = SettingsModel.Read();
			var login = SettingsModel.GetCredentials();

			var result = new NntpClient(new NntpConnection());
			try
			{
				await result.ConnectAsync(settings.Hostname, settings.Port, settings.SSL);
				var success = result.Authenticate(login.Username, login.Password);
				if (!success)
				{
					Console.WriteLine("nope");
				}
			}
			catch { }

			_clients.Add(result);

			return result;
		}
	}
}
