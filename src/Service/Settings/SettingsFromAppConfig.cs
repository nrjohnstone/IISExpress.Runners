using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISExpress.Host.Service.Settings
{
    internal class SettingsFromAppConfig
    {
        private static string GetValue(string key)
        {
            return ConfigurationManager.AppSettings.Get(key);
        }

        public string IISPath => GetValue("IISPath");
        public string WebSitePath => GetValue("WebSitePath");
        public string Port => GetValue("Port");
        public string ServiceDescription => GetValue("ServiceDescription");
        public string ServiceDisplayName => GetValue("ServiceDisplayName");
        public string ServiceName => GetValue("ServiceName");
    }
}
