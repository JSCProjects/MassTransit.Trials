using System;

namespace Messages;

public class SubmitOrderCommand
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = null!;
    public bool Error { get; set; }
}