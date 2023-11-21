using Azure.Messaging.ServiceBus;
using MassTransit;
using MassTransit.AzureServiceBusTransport.Configuration;
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
            services.AddMassTransit(x =>
            {
                x.UsingAzureServiceBus((brc, cfg) =>
                {
                    var settings = new HostSettings
                    {
                        ConnectionString = "changeMe"
                    };

                    cfg.Host(settings);

                    cfg.ReceiveEndpoint("order-service", e =>
                    {
                        e.Handler<SubmitOrder>(async context =>
                        {
                            await Console.Out.WriteLineAsync($"Submit Order Received: {context.Message.OrderId}");
                            await Task.Delay(TimeSpan.FromSeconds(1));
                        });
                        e.PrefetchCount = 1;
                    });
                });
            });
        });
}

