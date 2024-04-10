using Microsoft.Extensions.Configuration;

namespace UrlCheckerService
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<LinkChecker>();
                    services.AddHostedService<BackgroundServiceRunner>();
                });

            var host = builder.Build();
            await host.RunAsync();
        }
    }


}
