using NServiceBus;

namespace Messages
{
    public class OrderPlaced : IEvent
    {
        public string OrderId { get; set; }
    }

    public class StepOneExecuted : IEvent
    {
        public string ShittingSessionId { get; set; }
    }

    public class StepTwoExecuted : IEvent
    {
        public string ShittingSessionId { get; set; }
    }
}