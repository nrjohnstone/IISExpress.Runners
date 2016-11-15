using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using AmbientContext.LogService.Serilog;
using IISExpress.Host.Service.Settings;
using Topshelf;

namespace IISExpress.Host.Service
{
    internal class IISExpressHost
    {
        private Process _process;
        private Thread _iisMonitor;
        private readonly AmbientLogService _logger = new AmbientLogService();

        public bool Start(HostControl hostControl)
        {
            _hostControl = hostControl;
            var config = new SettingsFromAppConfig();

            string iisExpress = config.IISPath;
            
            StringBuilder arguments = new StringBuilder();
            string webSitePath = config.WebSitePath;
            string port = config.Port;

            arguments.Append($"/path:\"{webSitePath}\"");
            arguments.Append($" /Port:{port} /systray:false xxxx");

            _process = Process.Start(new ProcessStartInfo()
            {
                FileName = iisExpress,
                Arguments = arguments.ToString(),
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            _iisMonitor = new Thread(MonitorIISExpress) {Name = "IISMonitor"};
            _iisMonitor.Start();
            
            // Close stdout for Swagger to work properly
            _process.StandardOutput.Close();
            
            return true;
        }

        
        private void MonitorIISExpress()
        {
            while (!_shutdownMonitor)
            {
                if (_process.HasExited)
                {
                    Console.WriteLine("iisexpress has exited");
                    string msg = "iisexpress process has exited";
                    
                    _logger.Error(msg);
                    _hostControl.Stop();                    
                }
                Thread.Sleep(500);
            }
        }

        public void Stop()
        {
            try
            {
                _shutdownMonitor = true;
                _iisMonitor.Join(TimeSpan.FromSeconds(5));

                if (!_process.HasExited)
                    _process.Kill();
            }
            catch (Exception)
            {
            }
        }

        private bool _shutdownMonitor = false;
        private HostControl _hostControl;
    }
}