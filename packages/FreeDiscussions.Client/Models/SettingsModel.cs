using CredentialManagement;
using Newtonsoft.Json;
using System;
using System.IO;

namespace FreeDiscussions.Client.Models
{
    public class SettingsModel
	{
		public string Hostname { get; set; }
		public int Port { get; set; }
		public bool SSL { get; set; }
		public string DownloadFolder { get; set; }

		public static readonly string SettingsPath = System.IO.Path.Combine(Environment.CurrentDirectory, "settings.json");


		private const string target = "UsenetClientCredentials";

		public void Save()
		{
			string json = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(SettingsPath, json);
		}
		public static SettingsModel Read()
		{
			var content = File.ReadAllText(SettingsPath);
			return JsonConvert.DeserializeObject<SettingsModel>(content);
		}

		public static Login GetCredentials()
		{
			var cm = new Credential { Target = target };
			if (!cm.Load())
			{
				return null;
			}
			return new Login { Username = cm.Username, Password = cm.Password };
		}

		public static bool SetCredentials(
			 string username, string password)
		{
			return new Credential
			{
				Target = target,
				Username = username,
				Password = password,
				PersistanceType = PersistanceType.LocalComputer
			}.Save();
		}

		public static bool RemoveCredentials(string target)
		{
			return new Credential { Target = target }.Delete();
		}

		public class Login
		{
			public string Username { get; set; }
			public string Password { get; set; }
		}

	}
}
