using System;

namespace Messages;

public interface OrderAcceptedEvent
{
    Guid OrderId { get; }
    string OrderNumber { get; }
}