using System;
using System.Threading.Tasks;
using MassTransit;
using Messages;

namespace Consumers;

public class OrderReceivedEventConsumer :
    IConsumer<OrderReceivedEvent>
{
    public Task Consume(ConsumeContext<OrderReceivedEvent> context)
    {
        LogContext.Info?.Log("Audited Order: {OrderNumber} ({RedeliveryCount}, {RetryAttempt}, {RetryCount})",
            context.Message.OrderNumber,
            context.GetRedeliveryCount(),
            context.GetRetryAttempt(),
            context.GetRetryCount());
        throw new Exception("Ooops!");
    }
}