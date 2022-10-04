using FreeDiscussions.Plugin.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Usenet.Nntp;

namespace FreeDiscussions.Plugin
{
    /// <summary>
	/// Provides methods for checking NNTP connections as well as getting a configured client
	/// </summary>
    public class ConnectionManager
	{
        /// <summary>
		/// List of clients
		/// </summary>
		private static List<NntpClient> _clients = new List<NntpClient>();

        /// <summary>
		/// Checks a NNTP connection with the given credentials
		/// </summary>
		/// <param name="host">Hostname of the Usenet Provider</param>
		/// <param name="port">Port of the Connection</param>
		/// <param name="userName">Username to use</param>
		/// <param name="password">Password to use</param>
		/// <param name="useSSL">Use SSL Connection</param>
		/// <returns></returns>
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

        /// <summary>
		/// Checks the NNTP connection of the data the user has provided
		/// </summary>
		/// <returns></returns>
		public static Task<bool> CheckConnection()
		{
			Log.Information("CheckConnection...");
			var settings = SettingsModel.Read();
			var login = SettingsModel.GetCredentials();
			return CheckConnection(settings.Hostname, settings.Port, login.Username, login.Password, settings.SSL);
		}

        /// <summary>
		/// Get a configured NntpClient
		/// </summary>
		/// <returns></returns>
		public static async Task<Client> GetClient()
		{
			Log.Information("GetClient");

			var result = new Client();
			await result.Init();
			_clients.Add(result.Nntp);
			return result;
		}
	}

    /// <summary>
	/// NNTP Client
	/// </summary>
	public class Client : IDisposable
	{
        /// <summary>
		/// The NntpClient
		/// </summary>
		public NntpClient Nntp { get; private set; }

        /// <summary>
		/// Intialize the client with the data the user has provided
		/// </summary>
		/// <returns></returns>
		public async Task Init()
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
					throw new AuthException();
				}

				Nntp = result;
			}
			catch(Exception ex) {
				if (ex is AuthException) throw ex;
			}
		}

        /// <summary>
		/// Dispose the client
		/// </summary>
		public void Dispose()
		{
			try
			{
				this.Nntp.Quit();
			} 
			catch (Exception ex)
			{
				Console.WriteLine("Nooo");
			}
		}
	}

    /// <summary>
	/// Exception for failed authentication
	/// </summary>
	public class AuthException : Exception
	{
	}
}

