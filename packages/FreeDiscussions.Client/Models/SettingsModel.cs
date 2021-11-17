using CredentialManagement;
using Newtonsoft.Json;
using System.IO;

namespace FreeDiscussions.Client.Models
{
    public partial class SettingsModel
	{
		public string Hostname { get; set; }
		public int Port { get; set; }
		public bool SSL { get; set; }
		public string DownloadFolder { get; set; }


		private const string target = "UsenetClientCredentials";

		public void Save(string filename)
		{
			string json = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(filename, json);
		}
		public static SettingsModel Read(string filename)
		{
			var content = File.ReadAllText(filename);
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
