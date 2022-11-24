using System;
using System.Threading.Tasks;
using Consumers;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace OneEndpoint.MassTransit.Worker;

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
                        cfg.ReceiveEndpoint("OneEndpoint", configurator =>
                        {
                            configurator.ConfigureConsumers(context);
                            configurator.UseRawJsonSerializer();
                            configurator.UseRawJsonDeserializer();

                            configurator.UseDelayedRedelivery(c => c.Intervals(
                                TimeSpan.FromSeconds(5),
                                TimeSpan.FromSeconds(15)));

                            configurator.ConfigureDeadLetterQueueDeadLetterTransport();
                            configurator.ConfigureDeadLetterQueueErrorTransport();
                        });
                            
                        cfg.ConfigureEndpoints(context);

                        cfg.Host(hostContext.Configuration["ServiceBusConnection"]);
                    });
                });
            });
}