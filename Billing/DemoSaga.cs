using Messages;
using NServiceBus;
using System.Threading.Tasks;

namespace Billing
{
    public class DemoSaga : Saga<DemoSagaData>,
    IAmStartedByMessages<StartDemoSaga>,
    IHandleMessages<StepOneExecuted>,
    IHandleMessages<StepTwoExecuted>
    {
        public async Task Handle(StartDemoSaga msg, IMessageHandlerContext context)
        {
            InitializeSagaData(msg);

            await context.Send(new ExecuteStepOne
            {
                SagaId = msg.SagaId
            });
        }

            void InitializeSagaData(StartDemoSaga msg)
            {
                Data.SagaId = msg.SagaId;
                Data.Status = "Preparing";
            
            }

        public async Task Handle(StepOneExecuted msg, IMessageHandlerContext context)
        {
            Data.Status = "Done with one";
            await context.Send(new ExecuteStepTwo
            {
                ShittingSessionId = msg.ShittingSessionId
            });
        }

        public async Task Handle(StepTwoExecuted msg, IMessageHandlerContext context)
        {
            Data.Status = "Done with two";
            await context.Send(new LastStep
            {
                ShittingSessionId = msg.ShittingSessionId               
            });
            MarkAsComplete();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<DemoSagaData> mapper)
        {
            mapper.MapSaga(saga => saga.SagaId)
                .ToMessage<StartDemoSaga>(message => message.SagaId)
                .ToMessage<StepOneExecuted>(message => message.ShittingSessionId)
                .ToMessage<StepTwoExecuted>(message => message.ShittingSessionId);
        }
    }

    public class DemoSagaData : ContainSagaData
    {
        public string SagaId { get; set; }
        public string Status { get; set; }
    }
}
