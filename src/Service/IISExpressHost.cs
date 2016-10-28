using System;
using System.Diagnostics;
using System.Text;
using IISExpress.Host.Service.Settings;

namespace IISExpress.Host.Service
{
    internal class IISExpressHost
    {
        private Process _process;

        public void Start()
        {
            var config = new SettingsFromAppConfig();

            string iisExpress = config.IISPath;

            StringBuilder arguments = new StringBuilder();
            string webSitePath = config.WebSitePath;
            string port = config.Port;

            arguments.Append($"/path:\"{webSitePath}\"");
            arguments.Append($" /Port:{port} /systray:false");

            _process = Process.Start(new ProcessStartInfo()
            {
                FileName = iisExpress,
                Arguments = arguments.ToString(),
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            // Close stdout for Swagger to work properly
            _process.StandardOutput.Close();
        }

        public void Stop()
        {
            try
            {
                _process.Kill();
            }
            catch (Exception)
            {
            }
        }

    }
}