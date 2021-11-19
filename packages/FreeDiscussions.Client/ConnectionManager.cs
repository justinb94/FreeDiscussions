using FreeDiscussions.Client.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Usenet.Nntp;

namespace FreeDiscussions.Client
{
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

		public static async Task<NntpClient> GetClient()
		{
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
