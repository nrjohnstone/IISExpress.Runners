using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using AmbientContext.LogService.Serilog;
using IISExpress.Host.Service.Settings;
using Topshelf;

namespace IISExpress.Host.Service
{
    internal class IISExpressHost : IDisposable
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
            arguments.Append($" /Port:{port} /systray:false");
 
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

            _logger.Information($"[Start] IISExpress started with pid: {_process.Id}");

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
                Thread.Sleep(250);
                if (_process.HasExited)
                {
                    string msg = "IISExpress process has exited";
                    
                    _logger.Error(msg);
                    _hostControl.Stop();                    
                }
            }
        }

        public void Stop()
        {
            _logger.Information("[Stop] Enter");
            try
            {
                _shutdownMonitor = true;
                _iisMonitor.Join(TimeSpan.FromSeconds(5));

                if (!_process.HasExited)
                {
                    _logger.Debug("[Stop] Killing IISExpress");
                    _process.Kill();
                }                    
            }
            catch (Exception)
            {
            }
        }

        private bool _shutdownMonitor = false;
        private HostControl _hostControl;

        public void Dispose()
        {
            _logger.Debug("[Dispose] Waiting for IISExpress to exit");
            _process.WaitForExit(5000);
            _process?.Dispose();
        }
    }
}