using MassTransit;
using MassTransit.Azure.ServiceBus.Core.Configurators;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using mt.reconnect.contracts;

var host = CreateHostBuilder(args).Build();

var log = host.Services.GetRequiredService<ILogger<Program>>();

var timer = new Timer(_ =>
{
    var bc = host.Services.GetRequiredService<IBusControl>();
    var health = bc.CheckHealth();

    foreach (var (key, value) in health.Endpoints.Where(c => c.Key.Contains("order-service")))
    {
        log.LogWarning("{key} {status}", key, value.Status);
    }

}, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

await host.RunAsync();

static IHostBuilder CreateHostBuilder(string[] args)
{
    var provider = TokenProvider.CreateSharedAccessSignatureTokenProvider("##CHANGE_ME_SHARED_ACCESS_KEY##");
    
    return Host.CreateDefaultBuilder(args)
        .ConfigureLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = false;
                options.ColorBehavior = LoggerColorBehavior.Enabled;
                options.TimestampFormat = "hh:mm:ss ";
            });
        })
        .ConfigureServices((hostContext, services) =>
        {
            services.AddMassTransitHostedService();
            services.AddMassTransit(x =>
            {
                x.UsingAzureServiceBus((brc, cfg) =>
                {
                    var settings = new HostSettings
                    {
                        ServiceUri = new Uri("##CHANGE_ME_URI##"),
                        TokenProvider = provider,
                        TransportType = TransportType.AmqpWebSockets
                    };

                    cfg.Host(settings);

                    cfg.ReceiveEndpoint("order-service", e =>
                    {
                        e.Handler<SubmitOrder>(async context =>
                        {
                            await Console.Out.WriteLineAsync($"Submit Order Received: {context.Message.OrderId}");
                        });
                    });
                });
            });
        });
}

