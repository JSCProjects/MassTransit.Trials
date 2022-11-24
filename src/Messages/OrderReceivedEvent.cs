using System;

namespace Messages;

public interface OrderReceivedEvent
{
    Guid OrderId { get; }
    DateTime Timestamp { get; }

    string OrderNumber { get; }
}