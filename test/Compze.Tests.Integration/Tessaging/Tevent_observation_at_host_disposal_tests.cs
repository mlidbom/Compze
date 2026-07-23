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

using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Tessaging;

///<summary>The testing host's disposal contract for tevent observation: its at-rest wait covers the observation queues — a test<br/>
/// cannot pass with observation work in flight, and a queued observation is dispatched, never discarded — and a throwing<br/>
/// observer's failure, reported to the background-exception reporter when it ran, is rethrown when the host disposes.</summary>
public class Tevent_observation_at_host_disposal_tests : UniversalTestBase
{
#pragma warning disable CA2213 // Disposed in DisposeAsyncInternal through the base lifecycle, which the analyzer cannot see
   TestingEndpointHost _host = null!;
#pragma warning restore CA2213
   ExactlyOnceEndpoint _endpoint = null!;

   //TestingEndpointHost.DisposeAsync is idempotent, so the spec that disposes (and catches the rethrown failures) itself is not disturbed by this second call.
   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   void CreateHostWithAnEndpointObserving(Action<IMyGreetingRequestedTevent> observer)
   {
      _host = TestingEndpointHost.Create();
      _endpoint = _host.RegisterEndpoint(new ObservingEndpointDeclaration(observer));
   }

   class ObservingEndpointDeclaration : ExactlyOnceEndpointDeclaration<ObservingEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "Observing";
      public static EndpointId Id => new(Guid.Parse("2F8B5C1A-9D64-4E07-B3A2-6C815E9D40F7"));

      readonly Action<IMyGreetingRequestedTevent> _observer;
      internal ObservingEndpointDeclaration(Action<IMyGreetingRequestedTevent> observer) => _observer = observer;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      protected override void ObserveTevents(ITeventObservationRegistrar observe) =>
         observe.ForTevent<IMyGreetingRequestedTevent>((tevent, _) => _observer(tevent));
   }

   void PublishAnObservedTevent() =>
      _endpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
         unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MySpecialGreetingRequestedTevent()));

   [PCT] public async Task disposal_does_not_complete_while_an_observation_is_in_flight_and_the_queued_observation_is_dispatched_not_discarded()
   {
      var observerGate = IThreadGate.NewClosed(WaitTimeout.Seconds(10), "observerGate");
      CreateHostWithAnEndpointObserving(_ => observerGate.AwaitPassThrough());
      await _host.StartAsync();

      PublishAnObservedTevent();

      //The at-rest wait in DisposeAsync blocks the calling thread synchronously, so disposal runs on its own task while this
      //thread scripts the observer through the gate.
      var disposeTask = Task.Run(async () => await _host.DisposeAsync());
      observerGate.TryAwait(it => it.Queued == 1).Must().BeTrue();

      disposeTask.IsCompleted.Must().BeFalse();

      observerGate.Open();
      await disposeTask;
      observerGate.Passed.Must().Be(1);
   }

   [PCT] public async Task a_throwing_observers_failure_is_rethrown_when_the_host_disposes()
   {
      await CompzeLogger.SuppressLoggingWhileRunningAsync(async () =>
      {
         var observerException = new InvalidOperationException("2B7E9A04-51C3-4D68-8F1B-3A96D07E52C4");
         CreateHostWithAnEndpointObserving(_ => throw observerException);
         await _host.StartAsync();

         PublishAnObservedTevent();

         //No gate and no waiting before the dispose: the at-rest wait covers the queued observation, so by the time the
         //endpoints dispose the observer has run and its failure sits in the background-exception reporter.
         (await InvokingAsync(async () => await _host.DisposeAsync()).Must().ThrowAsync<AggregateException>())
            .Which.Flatten().InnerExceptions.Must().Satisfy(it => it.Contains(observerException));
      });
   }

   [PCT] public async Task every_failure_of_an_observer_that_throws_on_each_of_three_observed_tevents_is_collected_into_the_one_rethrown_AggregateException()
   {
      await CompzeLogger.SuppressLoggingWhileRunningAsync(async () =>
      {
         var failureCount = 0;
         CreateHostWithAnEndpointObserving(_ => throw new InvalidOperationException($"observer-failure-{Interlocked.Increment(ref failureCount)}"));
         await _host.StartAsync();

         PublishAnObservedTevent();
         PublishAnObservedTevent();
         PublishAnObservedTevent();

         //A throwing observer is reported and never retried; its queue moves on to the next observation, so three observed
         //tevents produce three reported failures, and the disposal rethrow carries every one of them.
         (await InvokingAsync(async () => await _host.DisposeAsync()).Must().ThrowAsync<AggregateException>())
            .Which.Flatten().InnerExceptions
            .Must().HaveCount(3)
            .Satisfy(it => it.Any(exception => exception.Message == "observer-failure-1"))
            .Satisfy(it => it.Any(exception => exception.Message == "observer-failure-2"))
            .Satisfy(it => it.Any(exception => exception.Message == "observer-failure-3"));
      });
   }
}
