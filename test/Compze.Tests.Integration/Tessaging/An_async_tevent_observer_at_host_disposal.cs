using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Must;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Integration.InProcess;
using Compze.Threading;
using Compze.Threading.Testing;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Tessaging;

///<summary>The testing host's disposal contract for tevent observation holds for an async observer as it does for a<br/>
/// synchronous one (<see cref="Tevent_observation_at_host_disposal_tests"/>): disposal waits for an observation in flight,<br/>
/// and a throwing observer's failure is rethrown when the host disposes.</summary>
public class An_async_tevent_observer_at_host_disposal : UniversalTestBase
{
#pragma warning disable CA2213 // Disposed in DisposeAsyncInternal through the base lifecycle, which the analyzer cannot see
   private protected TestingEndpointHost Host = null!;
#pragma warning restore CA2213
   ExactlyOnceEndpoint _endpoint = null!;

   //TestingEndpointHost.DisposeAsync is idempotent, so the specifications that dispose the host themselves are not disturbed by this second call.
   protected override async Task DisposeAsyncInternal() => await Host.DisposeAsync();

   private protected async Task StartAHostWithAnEndpointObservingAsync(Func<IMyGreetingRequestedTevent, Task> observer)
   {
      Host = TestingEndpointHost.Create();
      _endpoint = Host.RegisterEndpoint(new AsyncObservingEndpointDeclaration(observer));
      await Host.StartAsync();
   }

   private protected void PublishAnObservedTevent() =>
      _endpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
         unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MySpecialGreetingRequestedTevent()));

   class AsyncObservingEndpointDeclaration : ExactlyOnceEndpointDeclaration<AsyncObservingEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "AsyncObserving";
      public static EndpointId Id => new(Guid.Parse("7C3E9A21-5B8D-4F16-A0E4-92D1C6B8F357"));

      readonly Func<IMyGreetingRequestedTevent, Task> _observer;
      internal AsyncObservingEndpointDeclaration(Func<IMyGreetingRequestedTevent, Task> observer) => _observer = observer;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      protected override void ObserveTevents(ITeventObservationRegistrar observe) =>
         observe.ForTevent<IMyGreetingRequestedTevent>((tevent, _) => _observer(tevent));
   }

   public class that_awaits_a_closed_gate_when_the_host_disposes : An_async_tevent_observer_at_host_disposal
   {
      readonly IThreadGate _observerGate = IThreadGate.NewClosed(WaitTimeout.Seconds(10), nameof(_observerGate));
      bool _disposalCompletedWhileTheObserverAwaitedTheGate;

      protected override async Task InitializeAsyncInternal()
      {
         await StartAHostWithAnEndpointObservingAsync(_ => _observerGate.AwaitPassThroughAsync());
         PublishAnObservedTevent();

         var disposal = Host.DisposeAsync().AsTask();
         await _observerGate.AwaitQueueLengthEqualToAsync(1);
         //Time enough for a disposal that did not wait for the observation to complete.
         _disposalCompletedWhileTheObserverAwaitedTheGate = await disposal.CompletesWithinAsync(TimeSpan.FromMilliseconds(500));
         _observerGate.Open();
         await disposal;
      }

      [PCT] public void the_disposal_does_not_complete_while_the_observer_awaits() => _disposalCompletedWhileTheObserverAwaitedTheGate.Must().BeFalse();
      [PCT] public void the_observation_completes_before_the_disposal_does() => _observerGate.Passed.Must().Be(1);
   }

   public class that_throws_after_an_await : An_async_tevent_observer_at_host_disposal
   {
      readonly InvalidOperationException _observerException = new("5D1A8E37-2C94-4B6F-8A03-E7F2B9C4D618");
      AggregateException? _rethrownAtDisposal;

      protected override async Task InitializeAsyncInternal() =>
         await CompzeLogger.SuppressLoggingWhileRunningAsync(async () =>
         {
            await StartAHostWithAnEndpointObservingAsync(async _ =>
            {
               await Task.Yield();
               throw _observerException;
            });
            PublishAnObservedTevent();

            try
            {
               await Host.DisposeAsync();
            }
            catch(AggregateException exception)
            {
               _rethrownAtDisposal = exception;
            }
         });

      [PCT] public void its_failure_is_rethrown_when_the_host_disposes() =>
         _rethrownAtDisposal.Must().NotBeNull().Actual.Flatten().InnerExceptions.Must().Satisfy(it => it.Contains(_observerException));
   }
}
