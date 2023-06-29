using NServiceBus;

namespace Messages
{
    public class PlaceOrder : ICommand
    {
        public string OrderId { get; set; }
    }

    public class StartDemoSaga : ICommand
    {
        public string SagaId { get; set; }
    }

    public class ExecuteStepOne : ICommand
    {
        public string SagaId { get; set; }
    }

    public class ExecuteStepTwo : ICommand
    {
        public string ShittingSessionId { get; set; }
    }

    public class LastStep : ICommand
    {
        public string ShittingSessionId { get; set; }
    }
}