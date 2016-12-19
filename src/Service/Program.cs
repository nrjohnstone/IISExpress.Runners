using System;
using AmbientContext.LogService.Serilog;
using IISExpress.Host.Service.Settings;
using Serilog;
using Topshelf;

namespace IISExpress.Host.Service
{
    class Program
    {
        private static readonly AmbientLogService _logger = new AmbientLogService();

        static void Main(string[] args)
        {
            ConfigureSerilog();

            var settings = new SettingsFromAppConfig();

            HostFactory.Run(x =>
            {
                x.Service<IISExpressHost>(s =>
                {
                    s.ConstructUsing(name => new IISExpressHost());
                    s.WhenStarted((tc, hostControl) => tc.Start(hostControl));
                    s.WhenStopped(tc => tc.Stop());
                });

                if (settings.RunAsLocalService)
                    x.RunAsLocalService();

                x.OnException(OnException);
                x.SetDescription(settings.ServiceDescription);
                x.SetDisplayName(settings.ServiceDisplayName);
                x.SetServiceName(settings.ServiceName);
            });

            _logger.Information("IISExpress.Service exiting");
        }

        private static void OnException(Exception ex)
        {
            _logger.Error(ex, "{Exception}");
        }

        private static void ConfigureSerilog()
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();
        }
    }
}
