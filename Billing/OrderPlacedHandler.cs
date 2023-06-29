using System.Threading.Tasks;
using Messages;
using NServiceBus;
using NServiceBus.Logging;

namespace Billing
{

    public class OrderPlacedHandler :
        IHandleMessages<OrderPlaced>
    {
        static readonly ILog log = LogManager.GetLogger<OrderPlacedHandler>();

        public Task Handle(OrderPlaced message, IMessageHandlerContext context)
        {
            log.Info($"Billing has received OrderPlaced, OrderId = {message.OrderId}");
            return Task.CompletedTask;
        }
    }

    public class ExecuteStepOneHandler :  IHandleMessages<ExecuteStepOne>
    {
        static readonly ILog log = LogManager.GetLogger<ExecuteStepOneHandler>();

        public async Task Handle(ExecuteStepOne message, IMessageHandlerContext context)
        {
            log.Info($"Step one {message.SagaId}");
            await context.Publish(new StepOneExecuted { ShittingSessionId = message.SagaId });
        }
    }

    public class ExecuteStepTwoHandler : IHandleMessages<ExecuteStepTwo>
    {
        static readonly ILog log = LogManager.GetLogger<ExecuteStepTwoHandler>();

        public async Task Handle(ExecuteStepTwo message, IMessageHandlerContext context)
        {
            log.Info($"Step two {message.ShittingSessionId}");
            await context.Publish(new StepTwoExecuted { ShittingSessionId = message.ShittingSessionId });
        }
    }

    public class LastStepHandler : IHandleMessages<LastStep>
    {
        static readonly ILog log = LogManager.GetLogger<LastStepHandler>();

        public async Task Handle(LastStep message, IMessageHandlerContext context)
        {
            log.Info($"Gone!!! {message.ShittingSessionId}");
        }
    }
}