using System;
using System.Threading.Tasks;
using Consumers;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace SetEntityNameFormatter.MassTransit.Worker;

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
                        cfg.UseServiceBusMessageScheduler();

                        // cfg.MessageTopology.SetEntityNameFormatter(new MyEntityNameFormatter("Prefix"));
                        cfg.MessageTopology.SetEntityNameFormatter(
                            new PrefixEntityNameFormatter(
                                new MyEntityNameFormatter2(), "Prefix/"));
                        cfg.MessageTopology.SetEntityNameFormatter(new MessageUrnEntityNameFormatter());

                        cfg.ConfigureEndpoints(context, new DefaultEndpointNameFormatter(false));
                            
                        cfg.Host(hostContext.Configuration["ServiceBusConnection"]);
                    });
                    x.AddConfigureEndpointsCallback((_, configurator) =>
                    {
                        configurator.UseRawJsonSerializer();
                        configurator.UseRawJsonDeserializer();

                        // configurator.UseMessageRetry(r => r.Immediate(3));

                        configurator.UseDelayedRedelivery(c => c.Intervals(
                            TimeSpan.FromSeconds(5),
                            TimeSpan.FromSeconds(15)));

                        configurator.ConfigureDeadLetterQueueDeadLetterTransport();
                        configurator.ConfigureDeadLetterQueueErrorTransport();
                    });
                });
            });
}