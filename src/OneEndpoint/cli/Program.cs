using MassTransit;
using Messages;
using Microsoft.Extensions.Configuration;

var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

var builder = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json", true, true)
    .AddJsonFile($"appsettings.{environmentName}.json", true, true)
    .AddEnvironmentVariables();
var configuration = builder.Build();

var source = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var bus = Bus.Factory.CreateUsingAzureServiceBus(cfg =>
{
    cfg.UseRawJsonSerializer();
    cfg.UseRawJsonDeserializer();
    
    cfg.Host(configuration["ServiceBusConnection"]);
});

await bus.StartAsync(source.Token);

try
{
    while (true)
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        var value = await Task.Run(() =>
        {
            Console.WriteLine("Enter message (or quit to exit)");
            Console.Write("> ");
            return Console.ReadLine();
        });
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

        if ("quit".Equals(value, StringComparison.OrdinalIgnoreCase))
            break;

        EndpointConvention.Map<SubmitOrderCommand>(new Uri($"queue:OneEndpoint"));
        
        await bus.Send<SubmitOrderCommand>(new()
        {
            OrderNumber = "ABC",
            Error = bool.Parse(value!),
            OrderId = Guid.NewGuid()
        });
    }
}
finally
{
    await bus.StopAsync();
}