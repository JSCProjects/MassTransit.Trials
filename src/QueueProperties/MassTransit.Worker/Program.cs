using System;
using System.Threading.Tasks;
using Consumers;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace QueueProperties.MassTransit.Worker;

public class Program
{
    public static async Task Main(string[] args)
    {
        await CreateHostBuilder(args).Build().RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddMassTransit(x =>
                {
                    x.AddConsumersFromNamespaceContaining<ConsumerNamespace>();
                    x.UsingAzureServiceBus((context, cfg) =>
                    {
                        cfg.LockDuration = TimeSpan.FromSeconds(30);
                        cfg.EnableDeadLetteringOnMessageExpiration = false;
                        // cfg.SupportOrdering = false; Property doesn't exist.
                        cfg.DefaultMessageTimeToLive = TimeSpan.FromMinutes(5);
                        cfg.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(4);
                        cfg.AutoDeleteOnIdle = TimeSpan.MaxValue;
                        cfg.MaxDeliveryCount = 10;
                            
                        cfg.ConfigureEndpoints(context);

                        cfg.Host(hostContext.Configuration["ServiceBusConnection"]);
                    });
                });
            });
}