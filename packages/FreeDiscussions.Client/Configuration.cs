using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FreeDiscussions.Client
{

    public class ConfigurationData
    {
        public List<PluginConfigurationData> plugins { get; set; }
    }
    
    public class PluginConfigurationData
    {
        public string Name { get; set; }
        public string uri { get; set; }
        public Dictionary<string, string> config { get; set; }


        public bool IsValid()
        {
            // nothing set
            if (string.IsNullOrEmpty(Name))
            {
                return false;
            }

            return true;
        }
    }




    internal class Configuration
    {
        public static string Schema = @"{
              'type': 'object',
              'properties':
              {
                'plugins': {
                  'type': 'array',
                  'items': {'type':'dynamic'}
                }
              }
            }";
    }
}
//