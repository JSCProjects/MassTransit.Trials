using AutoFixture;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace UnitTests.Sagas;

public class StateMachineWithActivityTests : IAsyncLifetime
{
    private readonly Mock<IMyService> _myServiceMock = new();
    private readonly Fixture _fixture = new();
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<StateMachineWithActivity, MyState> _sagaHarness = null!;
    private ServiceProvider _provider = null!;
    private IServiceCollection _serviceCollection = null!;

    [Fact]
    public async Task Initially_WhenSomethingRegisteredEvent_ThenCurrentStateIsRegisteredAndActivityShouldBeTriggered()
    {
        // Arrange
        var message = _fixture.Create<SomethingRegisteredEvent>();

        // Act
        await _harness.Bus.Publish(message);

        // Assert
        Assert.True(await _harness.Consumed.Any<SomethingRegisteredEvent>());
        Assert.True(await _sagaHarness.Consumed.Any<SomethingRegisteredEvent>());

        var sagas = _sagaHarness.Sagas.SelectAsync(_ => true, CancellationToken.None);
        Assert.NotNull(sagas);
        var instance = (await sagas.First()).Saga;
        Assert.Equal(1, await sagas.Count());

        Assert.Equal(message.Key1, instance.Key1);
        Assert.Equal(message.Key2, instance.Key2);
        Assert.Equal(nameof(StateMachineWithActivity.Registered), instance.CurrentState);

        _myServiceMock.Verify(x => x.DoSomethingFromMyService(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    public Task InitializeAsync()
    {
        _serviceCollection = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<StateMachineWithActivity, MyState>();
            })
            .AddTransient<IMyService>(_ => _myServiceMock.Object);

        _provider = _serviceCollection
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();

        _sagaHarness = _harness.GetSagaStateMachineHarness<StateMachineWithActivity, MyState>();

        return _harness.Start();
    }

    public Task DisposeAsync()
    {
        return _harness.Stop();
    }

    class StateMachineWithActivity : MassTransitStateMachine<MyState>
    {
        public State Registered { get; } = null!;
        public State SomethingWrong { get; } = null!;

        public Event<SomethingRegisteredEvent> SomethingRegisteredEvent { get; } = null!;

        public Schedule<MyState, TimeoutExceededMessage> TimeoutExceeded { get; } = null!;

        public StateMachineWithActivity()
        {
            Event(() => SomethingRegisteredEvent, e =>
                e.CorrelateBy((state, context) => state.Key1 == context.Message.Key1 &&
                                                  state.Key2 == context.Message.Key2)
                    .SelectId(context => context.CorrelationId ?? NewId.NextGuid()));

            Schedule(() => TimeoutExceeded, state => state.TimeoutExceededTokenId,
                configurator =>
                {
                    configurator.Received = r => r.CorrelateById(
                        context => context.Message.CorrelationId);
                });

            InstanceState(x => x.CurrentState);

            Initially(
                When(SomethingRegisteredEvent)
                    .Then(context =>
                    {
                        context.Saga.Key1 = context.Message.Key1;
                        context.Saga.Key2 = context.Message.Key2;
                    })
                    .Activity(x => x.OfInstanceType<MyActivity>())
                    .TransitionTo(Registered));

            WhenEnter(SomethingWrong, binder =>
                binder.Schedule(TimeoutExceeded, context =>
                    context.Init<TimeoutExceededMessage>(new
                    {
                        context.Saga.CorrelationId
                    }), _ => DateTime.UtcNow.AddMinutes(5)));

            During(SomethingWrong,
                When(TimeoutExceeded.Received)
                    .Activity(x => x.OfInstanceType<MyActivity>())
                    .TransitionTo(Registered));
        }
    }

    class MyState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; }
        public string Key1 { get; set; }
        public string Key2 { get; set; }
        public Guid? TimeoutExceededTokenId { get; set; }
    }

    record SomethingRegisteredEvent
    {
        public string Key1 { get; init; }
        public string Key2 { get; init; }
    }

    record TimeoutExceededMessage : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; init; }
    }

    class MyActivity : IStateMachineActivity<MyState>
    {
        private readonly IMyService _myService;

        public MyActivity(IMyService myService)
        {
            _myService = myService;
        }

        public void Probe(ProbeContext context)
        {
            context.CreateScope(nameof(MyActivity));
        }

        public void Accept(StateMachineVisitor visitor)
        {
            visitor.Visit(this);
        }

        public async Task Execute(BehaviorContext<MyState> context, IBehavior<MyState> next)
        {
            await DoSomething(context).ConfigureAwait(false);
            await next.Execute(context).ConfigureAwait(false);
        }

        public async Task Execute<T>(BehaviorContext<MyState, T> context, IBehavior<MyState, T> next) where T : class
        {
            await DoSomething(context).ConfigureAwait(false);
            await next.Execute(context).ConfigureAwait(false);
        }

        public Task Faulted<TException>(BehaviorExceptionContext<MyState, TException> context, IBehavior<MyState> next) where TException : Exception
        {
            return next.Faulted(context);
        }

        public Task Faulted<T, TException>(BehaviorExceptionContext<MyState, T, TException> context, IBehavior<MyState, T> next) where T : class where TException : Exception
        {
            return next.Faulted(context);
        }

        private Task DoSomething(BehaviorContext<MyState> context)
        {
            return _myService.DoSomethingFromMyService(context.Saga.Key1, context.Saga.Key2);
        }
    }

    interface IMyService
    {
        Task DoSomethingFromMyService(string key1, string key2);
    }
}

