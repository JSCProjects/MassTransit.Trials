using System;
using System.Threading.Tasks;
using MassTransit;
using Messages;

namespace Consumers;

public class SubmitOrderCommandConsumer :
    IConsumer<SubmitOrderCommand>
{
    public async Task Consume(ConsumeContext<SubmitOrderCommand> context)
    {
        LogContext.Info?.Log("Processing Order: {OrderNumber} ({RedeliveryCount}, {RetryAttempt}, {RetryCount})", 
            context.Message.OrderNumber, 
            context.GetRedeliveryCount(),
            context.GetRetryAttempt(),
            context.GetRetryCount());

        if (context.Message.Error)
        {
            throw new Exception("Erroer!");
        }

        await context.Publish<OrderReceivedEvent>(new
        {
            context.Message.OrderId,
            context.Message.OrderNumber,
            Timestamp = DateTime.UtcNow
        });
    }
}