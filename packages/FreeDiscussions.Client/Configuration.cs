using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FreeDiscussions.Client
{

    class ConfigurationData
    {
        public List<PluginConfigurationData> plugins { get; set; }
    }
    
    class PluginConfigurationData
    {
        public string Name { get; set; }
        public string uri { get; set; }
        public Dictionary<string, string> config { get; set; }


        public bool IsValid()
        {
            // nothing set
            if (string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(uri))
            {
                return false;
            }

            // both set
            if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(uri))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(Name))
            {
                // official plugin
            } 
            else
            {
                // third party plugin
            }

            return true;
        }
    }

    
    internal class Configuration
    {
        public static async Task doIt()
        {
            var schema = @"{
              'type': 'object',
              'properties':
              {
                'plugins': {
                  'type': 'array',
                  'items': {'type':'PluginConfigurationData'}
                }
              }
            }";

            var yaml = @"
                plugins: 
                  - name: Musik
                  - name: Und so
                    uri: http://leck.de
                    config:
                      Hello: World
                ";

            var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

            try
            {
                var p = deserializer.Deserialize<ConfigurationData>(yaml);
                Console.WriteLine(p);
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    // yaml 
    // => json
    // validate
    

    // signatur 
    
}
