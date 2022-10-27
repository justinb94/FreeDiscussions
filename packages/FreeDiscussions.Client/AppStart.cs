using FreeDiscussions.Client.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NamingConventions;

namespace FreeDiscussions.Client
{
    internal class AppStart
    {
        public static ConfigurationData Config;
        private static SplashScreen SplashScreenWnd;

        public static async Task<bool> Run(SplashScreen wnd)
        {
            // show splash screen
            SplashScreenWnd = wnd;

            wnd.Dispatcher.Invoke(() =>
            {
                SplashScreenWnd.Show();
                SplashScreenWnd.SetStatusText("loading config");
            });

            // load config file, validate etc.
            var configFilePath = System.IO.Path.Combine(Environment.CurrentDirectory, "config.yaml");
            if (!File.Exists(configFilePath))
            {
                var shouldContinue = wnd.Dispatcher.Invoke<bool>(() =>
                {
                    if (!SplashScreenWnd.ConfigFileMissing())
                    {
                        SplashScreenWnd.Close();
                        return false;
                    }
                    else
                    {
                        // create empty config file
                        var serializer = new YamlDotNet.Serialization.SerializerBuilder().WithNamingConvention(new CamelCaseNamingConvention()).Build();
                        var content = serializer.Serialize(new ConfigurationData());
                        File.WriteAllText(configFilePath, content);
                        return true;
                    }
                });
                if (!shouldContinue)
                    return false;
            }


            // load config file into string
            var config = System.IO.File.ReadAllText(configFilePath);

            wnd.Dispatcher.Invoke(() =>
            {
                SplashScreenWnd.SetStatusText("validating config");
            });

            // validate config
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();
            Config = deserializer.Deserialize<ConfigurationData>(config);
            if (Config == null)
            {

                var shouldContinue = wnd.Dispatcher.Invoke<bool>(() =>
                {
                    if (!SplashScreenWnd.ConfigFileInvalid())
                    {
                        SplashScreenWnd.Close();
                        return false;
                    }
                    return true;
                });

                if (!shouldContinue) return false;
            }

            // create local plugins directory, if not exists 
            var pluginsDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "plugins\\");
            if (!Directory.Exists(pluginsDirectory))
            {
                Directory.CreateDirectory(pluginsDirectory);
            }

            wnd.Dispatcher.Invoke(() =>
            {
                SplashScreenWnd.SetStatusText("find installed plugins");
            });

            // find local plugins
            var localPlugins = Directory.GetDirectories(pluginsDirectory).Select(x =>
            {

                var dir = new DirectoryInfo(x);
                return new LocalPluginInfo
                {
                    Name = dir.Name.IndexOf("-") != -1 ? dir.Name.Split("-").First() : dir.Name,
                    Version = dir.Name.IndexOf("-") != -1 ? Version.Parse(dir.Name.Split("-").Last()) : new Version(0, 0, 0, 0),
                    Folder = new DirectoryInfo(x)
                };
            }
            ).ToList();
        


            // try download missing plugins
            if (Config.plugins != null)
            {
                wnd.Dispatcher.Invoke(() =>
                {
                    SplashScreenWnd.SetStatusText("getting releases");
                });
                
                // get official releases
                List<Release> officialReleases = new List<Release>();
                var webRequest = WebRequest.Create("https://api.github.com/repos/justinb94/FreeDiscussions/releases") as HttpWebRequest;
                if (webRequest == null)
                {
                    // cant check for latest version
                    wnd.Dispatcher.Invoke(() =>
                    {
                        SplashScreenWnd.Offline();
                        SplashScreenWnd.Close();
                    });
                    return true;
                }
                webRequest.ContentType = "application/json";
                webRequest.UserAgent = "Nothing";

                using (var s = webRequest.GetResponse().GetResponseStream())
                {
                    using (var sr = new StreamReader(s))
                    {
                        var releasesJson = sr.ReadToEnd();
                        officialReleases = JsonConvert.DeserializeObject<List<Release>>(releasesJson);
                        Console.WriteLine(officialReleases);
                    }
                }

                // iterate through plugins

                foreach (var plugin in Config.plugins)
                {
                    Release latestRelease = null;
                    var installedPlugin = localPlugins.FirstOrDefault(x => x.Name == plugin.Name);

                    // get latest release
                    if (String.IsNullOrEmpty(plugin.uri))
                    {
                        // Official plugin
                        latestRelease = officialReleases.FirstOrDefault(x => x.TagName.ToUpper().StartsWith(plugin.Name.ToUpper() + "-"));
                    }
                    else
                    {
                        wnd.Dispatcher.Invoke(() =>
                        {
                            SplashScreenWnd.SetStatusText("getting latest version for plugin " + plugin.Name);
                        });

                        try
                        {
                            // third party plugin
                            var webRequest2 = WebRequest.Create(plugin.uri) as HttpWebRequest;
                            if (webRequest2 == null)
                            {
                                // cant check for latest version
                                if (installedPlugin == null)
                                {
                                    wnd.Dispatcher.Invoke(() =>
                                    {
                                        SplashScreenWnd.CantGetLatestVersionFromUrl(plugin);
                                        SplashScreenWnd.Close();
                                    });
                                    return false;
                                }
                                return true;
                            }

                            webRequest2.ContentType = "application/json";
                            webRequest2.UserAgent = "Nothing";

                            using (var s = webRequest2.GetResponse().GetResponseStream())
                            {
                                using (var sr = new StreamReader(s))
                                {
                                    var releasesJson = sr.ReadToEnd();
                                    var releases = JsonConvert.DeserializeObject<List<Release>>(releasesJson);
                                    latestRelease = releases.FirstOrDefault(x => x.TagName.ToUpper().StartsWith(plugin.Name.ToUpper() + "-"));
                                    Console.WriteLine(releases);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (installedPlugin == null)
                            {
                                wnd.Dispatcher.Invoke(() =>
                                {
                                    SplashScreenWnd.CantGetLatestVersionFromUrl(plugin);
                                    SplashScreenWnd.Close();
                                });
                                return false;
                            }
                            return true;
                        }
                    }

                    if (latestRelease == default(Release))
                    {
                        wnd.Dispatcher.Invoke(() =>
                        {
                            SplashScreenWnd.UnknownPlugin(plugin);
                            SplashScreenWnd.Close();
                        });
                        return false;
                    }


                    var downloadUrl = String.Empty;
                    Version version = null;

                    var asset = latestRelease.Assets.FirstOrDefault();
                    var tagName = latestRelease.TagName;
                    if (asset != default(Asset) && Version.TryParse(tagName.Split('-')[1], out version))
                    {
                        downloadUrl = asset.BrowserDownloadUrl;
                    }

                    // check if plugin is already installed
                    if (installedPlugin == null)
                    {
                        // not yet installed
                        installPlugin(plugin, version, downloadUrl);
                    }
                    else
                    {
                        // already installed, check version
                        if (installedPlugin.Version.CompareTo(version) < 0)
                        {
                            // delete old folder
                            Directory.Delete(installedPlugin.Folder.FullName, true);

                            // install new version
                            installPlugin(plugin, version, downloadUrl);
                        }
                    }
                }
            }

            wnd.Dispatcher.Invoke(() =>
            {
                SplashScreenWnd.Close();
            });
            
            return true;
        }
        
        private static void installPlugin(PluginConfigurationData plugin, Version version, string downloadUrl)
        {
            SplashScreenWnd.Dispatcher.Invoke(() =>
            {
                SplashScreenWnd.SetStatusText("downloading plugin " + plugin.Name);
            });

            WebClient client = new WebClient();
            var fileName = System.IO.Path.GetFileName(new Uri(downloadUrl).LocalPath);
            var target = System.IO.Path.Combine(Environment.CurrentDirectory, "plugins\\", fileName);
            client.DownloadFile(new Uri(downloadUrl), target);

            SplashScreenWnd.Dispatcher.Invoke(() =>
            {
                SplashScreenWnd.SetStatusText("extracting plugin " + plugin.Name);
            });
            
            // unzip
            string extractPath = System.IO.Path.Combine(Environment.CurrentDirectory, "plugins\\", plugin.Name + "-" + version.ToString());
            System.IO.Compression.ZipFile.ExtractToDirectory(target, extractPath);

            SplashScreenWnd.Dispatcher.Invoke(() =>
            {
                SplashScreenWnd.SetStatusText("cleaning up");
            });

            File.Delete(target);
        }
    }
}

public class LocalPluginInfo
{
    public Version Version { get; set; }
    public string Name { get; set; }
    public DirectoryInfo Folder { get; set; }
}

// Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
public class Asset
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("node_id")]
    public string NodeId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("label")]
    public string Label { get; set; }

    [JsonProperty("uploader")]
    public Uploader Uploader { get; set; }

    [JsonProperty("content_type")]
    public string ContentType { get; set; }

    [JsonProperty("state")]
    public string State { get; set; }

    [JsonProperty("size")]
    public int Size { get; set; }

    [JsonProperty("download_count")]
    public int DownloadCount { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonProperty("browser_download_url")]
    public string BrowserDownloadUrl { get; set; }
}

public class Author
{
    [JsonProperty("login")]
    public string Login { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("node_id")]
    public string NodeId { get; set; }

    [JsonProperty("avatar_url")]
    public string AvatarUrl { get; set; }

    [JsonProperty("gravatar_id")]
    public string GravatarId { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("html_url")]
    public string HtmlUrl { get; set; }

    [JsonProperty("followers_url")]
    public string FollowersUrl { get; set; }

    [JsonProperty("following_url")]
    public string FollowingUrl { get; set; }

    [JsonProperty("gists_url")]
    public string GistsUrl { get; set; }

    [JsonProperty("starred_url")]
    public string StarredUrl { get; set; }

    [JsonProperty("subscriptions_url")]
    public string SubscriptionsUrl { get; set; }

    [JsonProperty("organizations_url")]
    public string OrganizationsUrl { get; set; }

    [JsonProperty("repos_url")]
    public string ReposUrl { get; set; }

    [JsonProperty("events_url")]
    public string EventsUrl { get; set; }

    [JsonProperty("received_events_url")]
    public string ReceivedEventsUrl { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("site_admin")]
    public bool SiteAdmin { get; set; }
}

public class Release
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("assets_url")]
    public string AssetsUrl { get; set; }

    [JsonProperty("upload_url")]
    public string UploadUrl { get; set; }

    [JsonProperty("html_url")]
    public string HtmlUrl { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("author")]
    public Author Author { get; set; }

    [JsonProperty("node_id")]
    public string NodeId { get; set; }

    [JsonProperty("tag_name")]
    public string TagName { get; set; }

    [JsonProperty("target_commitish")]
    public string TargetCommitish { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("draft")]
    public bool Draft { get; set; }

    [JsonProperty("prerelease")]
    public bool Prerelease { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonProperty("assets")]
    public List<Asset> Assets { get; set; }

    [JsonProperty("tarball_url")]
    public string TarballUrl { get; set; }

    [JsonProperty("zipball_url")]
    public string ZipballUrl { get; set; }

    [JsonProperty("body")]
    public string Body { get; set; }
}

public class Uploader
{
    [JsonProperty("login")]
    public string Login { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("node_id")]
    public string NodeId { get; set; }

    [JsonProperty("avatar_url")]
    public string AvatarUrl { get; set; }

    [JsonProperty("gravatar_id")]
    public string GravatarId { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("html_url")]
    public string HtmlUrl { get; set; }

    [JsonProperty("followers_url")]
    public string FollowersUrl { get; set; }

    [JsonProperty("following_url")]
    public string FollowingUrl { get; set; }

    [JsonProperty("gists_url")]
    public string GistsUrl { get; set; }

    [JsonProperty("starred_url")]
    public string StarredUrl { get; set; }

    [JsonProperty("subscriptions_url")]
    public string SubscriptionsUrl { get; set; }

    [JsonProperty("organizations_url")]
    public string OrganizationsUrl { get; set; }

    [JsonProperty("repos_url")]
    public string ReposUrl { get; set; }

    [JsonProperty("events_url")]
    public string EventsUrl { get; set; }

    [JsonProperty("received_events_url")]
    public string ReceivedEventsUrl { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("site_admin")]
    public bool SiteAdmin { get; set; }
}

