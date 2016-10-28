using AmbientContext.LogService.Serilog;
using IISExpress.Host.Service.Settings;
using Serilog;
using Topshelf;

namespace IISExpress.Host.Service
{
    class Program
    {
        private AmbientLogService _logger = new AmbientLogService();

        static void Main(string[] args)
        {
            ConfigureSerilog();

            var settings = new SettingsFromAppConfig();

            HostFactory.Run(x =>
            {
                x.Service<IISExpressHost>(s =>
                {
                    s.ConstructUsing(name => new IISExpressHost());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription(settings.ServiceDescription);
                x.SetDisplayName(settings.ServiceDisplayName);
                x.SetServiceName(settings.ServiceName);
            });
        }

        private static void ConfigureSerilog()
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();
        }
    }
}
