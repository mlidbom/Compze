using System.Collections.Concurrent;
using System.Transactions;
using Compze.DependencyInjection;
using Compze.Internals.SystemCE.TransactionsCE.Testing;
using Compze.Must;
using Compze.Tessaging.Engine;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Integration.InProcess;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Engine;

///<summary>Dynamic observers, created through <see cref="IDynamicTeventObservers"/>: they observe as composed observers do, from<br/>
/// their creation until they are disposed. A plain container has no other endpoints, so nothing is subscribed to for them: they<br/>
/// may observe any tevent type.</summary>
public partial class Given_a_container_composing_a_LocalTessagingEngine
{
   IDynamicTeventObservers DynamicTeventObservers => Container.RootResolver.Resolve<IDynamicTeventObservers>();

   ILocalTessagingEngine Engine => Container.RootResolver.Resolve<ILocalTessagingEngine>();

   void PublishInAUnitOfWork(params IEnumerable<ITevent> tevents) =>
      Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork =>
      {
         var publisher = unitOfWork.Resolve<IUnitOfWorkTeventPublisher>();
         foreach(var tevent in tevents) publisher.Publish(tevent);
      });

   public class with_a_dynamic_observer_of_numbered_tevents : Given_a_container_composing_a_LocalTessagingEngine
   {
      ConcurrentQueue<int> ObservedSequenceNumbers { get; } = [];
      IDisposable Observation { get; }

      protected with_a_dynamic_observer_of_numbered_tevents()
      {
         ComposeContainerWithEngine(_ => {});
         Observation = DynamicTeventObservers.ObserveTevents(observe => observe.ForTevent((IMyNumberedTevent tevent) => ObservedSequenceNumbers.Enqueue(tevent.SequenceNumber)));
      }

      public class after_a_unit_of_work_that_rolls_back_publishes_tevent_1_and_one_that_commits_publishes_tevent_2 : with_a_dynamic_observer_of_numbered_tevents
      {
         public after_a_unit_of_work_that_rolls_back_publishes_tevent_1_and_one_that_commits_publishes_tevent_2()
         {
            Invoking(() => Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork =>
                          {
                             Transaction.Current!.FailOnPrepare();
                             unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MyNumberedTevent(1));
                          }))
                         .Must().Throw<TransactionAbortedException>();
            PublishInAUnitOfWork(new MyNumberedTevent(2));
         }

         protected override async Task InitializeAsyncInternal() => await Engine.AwaitNoTessagesInFlightAsync();

         [PCT] public void it_observes_only_the_committed_tevent_2() => ObservedSequenceNumbers.Must().SequenceEqual([2]);
      }

      public class after_tevent_1_is_published_then_the_observer_is_disposed_twice_then_tevent_2_is_published : with_a_dynamic_observer_of_numbered_tevents
      {
         public after_tevent_1_is_published_then_the_observer_is_disposed_twice_then_tevent_2_is_published() => PublishInAUnitOfWork(new MyNumberedTevent(1));

         protected override async Task InitializeAsyncInternal()
         {
            await Engine.AwaitNoTessagesInFlightAsync();
            Observation.Dispose();
            Observation.Dispose();
            PublishInAUnitOfWork(new MyNumberedTevent(2));
            await Engine.AwaitNoTessagesInFlightAsync();
         }

         [PCT] public void it_observed_only_tevent_1() => ObservedSequenceNumbers.Must().SequenceEqual([1]);
      }
   }

   public class with_an_async_dynamic_observer_that_awaits_before_recording_each_tevent : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly ConcurrentQueue<int> _observedSequenceNumbers = [];
      int[] _observedWhenTheWaitCompleted = [];

      public with_an_async_dynamic_observer_that_awaits_before_recording_each_tevent()
      {
         ComposeContainerWithEngine(_ => {});
         DynamicTeventObservers.ObserveTevents(observe => observe.ForTevent(async (IMyNumberedTevent tevent) =>
         {
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            _observedSequenceNumbers.Enqueue(tevent.SequenceNumber);
         }));
         for(var sequenceNumber = 1; sequenceNumber <= 3; sequenceNumber++) PublishInAUnitOfWork(new MyNumberedTevent(sequenceNumber));
      }

      protected override async Task InitializeAsyncInternal()
      {
         await Engine.AwaitNoTessagesInFlightAsync();
         _observedWhenTheWaitCompleted = [.._observedSequenceNumbers];
      }

      [PCT] public void awaiting_no_tessages_in_flight_waits_until_it_has_observed_every_tevent_in_publish_order() =>
         _observedWhenTheWaitCompleted.Must().SequenceEqual([1, 2, 3]);
   }

   public class with_a_dynamic_observer_that_disposes_its_own_observation_when_it_observes_a_tevent : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly ConcurrentQueue<int> _observedSequenceNumbers = [];
      readonly IDisposable? _observation;
      bool _disposingFromItsOwnCallReturned;

      public with_a_dynamic_observer_that_disposes_its_own_observation_when_it_observes_a_tevent()
      {
         ComposeContainerWithEngine(_ => {});
         _observation = DynamicTeventObservers.ObserveTevents(observe => observe.ForTevent((IMyNumberedTevent tevent) =>
         {
            _observedSequenceNumbers.Enqueue(tevent.SequenceNumber);
            _observation!.Dispose(); //Assigned before the tevent it observes is published.
            _disposingFromItsOwnCallReturned = true;
         }));
         PublishInAUnitOfWork(new MyNumberedTevent(1));
      }

      protected override async Task InitializeAsyncInternal()
      {
         await Engine.AwaitNoTessagesInFlightAsync();
         PublishInAUnitOfWork(new MyNumberedTevent(2));
         await Engine.AwaitNoTessagesInFlightAsync();
      }

      [PCT] public void disposing_from_its_own_call_returns() => _disposingFromItsOwnCallReturned.Must().BeTrue();
      [PCT] public void it_observes_nothing_after_it_disposed_itself() => _observedSequenceNumbers.Must().SequenceEqual([1]);
   }

   public class with_dynamic_observers_of_a_tevents_base_interface_and_of_an_unrelated_interface : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly ConcurrentQueue<IMyGreetingRequestedTevent> _observedByTheObserverOfTheBaseInterface = [];
      readonly ConcurrentQueue<IMyUnrelatedTevent> _observedByTheObserverOfTheUnrelatedInterface = [];
      readonly MySpecialGreetingRequestedTevent _publishedTevent = new();

      public with_dynamic_observers_of_a_tevents_base_interface_and_of_an_unrelated_interface()
      {
         ComposeContainerWithEngine(_ => {});
         DynamicTeventObservers.ObserveTevents(observe => observe
                                            .ForTevent((IMyGreetingRequestedTevent tevent) => _observedByTheObserverOfTheBaseInterface.Enqueue(tevent))
                                            .ForTevent((IMyUnrelatedTevent tevent) => _observedByTheObserverOfTheUnrelatedInterface.Enqueue(tevent)));
         PublishInAUnitOfWork(_publishedTevent);
      }

      protected override async Task InitializeAsyncInternal() => await Engine.AwaitNoTessagesInFlightAsync();

      [PCT] public void the_observer_of_the_base_interface_observes_the_published_tevent() => _observedByTheObserverOfTheBaseInterface.Single().Must().ReferenceEqual(_publishedTevent);
      [PCT] public void the_observer_of_the_unrelated_interface_observes_nothing() => _observedByTheObserverOfTheUnrelatedInterface.Must().BeEmpty();
   }

   public class with_a_dynamic_observer_that_throws_when_it_observes_tevent_1 : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly ConcurrentQueue<int> _observedSequenceNumbers = [];

      public with_a_dynamic_observer_that_throws_when_it_observes_tevent_1()
      {
         ComposeContainerWithEngine(_ => {});
         DynamicTeventObservers.ObserveTevents(observe => observe.ForTevent((IMyNumberedTevent tevent) =>
         {
            if(tevent.SequenceNumber == 1) throw new Exception("thrown by the dynamic observer");
            _observedSequenceNumbers.Enqueue(tevent.SequenceNumber);
         }));
         PublishInAUnitOfWork(new MyNumberedTevent(1));
         PublishInAUnitOfWork(new MyNumberedTevent(2));
      }

      protected override async Task InitializeAsyncInternal() => await Engine.AwaitNoTessagesInFlightAsync();

      [PCT] public void it_still_observes_tevent_2() => _observedSequenceNumbers.Must().SequenceEqual([2]);
   }

   public class with_dynamic_observers_of_each_unit_of_works_tevents_and_of_each_tevent_and_nothing_subscribed_for_them : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly ConcurrentQueue<IMySpecialGreetingRequestedTevent> _observedTevents = [];
      readonly ConcurrentQueue<IReadOnlyList<IMyGreetingRequestedTevent>> _observedLists = [];
      readonly MySpecialGreetingRequestedTevent _publishedTevent = new();

      public with_dynamic_observers_of_each_unit_of_works_tevents_and_of_each_tevent_and_nothing_subscribed_for_them()
      {
         ComposeContainerWithEngine(_ => {});
         DynamicTeventObservers.ObserveTevents(observe => observe
                                            .ForTevent((IMySpecialGreetingRequestedTevent tevent) => _observedTevents.Enqueue(tevent))
                                            .ForTeventsOfEachUnitOfWork((IReadOnlyList<IMyGreetingRequestedTevent> tevents) => _observedLists.Enqueue(tevents)));
         PublishInAUnitOfWork(_publishedTevent);
      }

      protected override async Task InitializeAsyncInternal() => await Engine.AwaitNoTessagesInFlightAsync();

      [PCT] public void the_observer_of_each_tevent_observes_the_published_tevent() => _observedTevents.Single().Must().ReferenceEqual(_publishedTevent);
      [PCT] public void the_observer_of_each_unit_of_works_tevents_observes_it_in_a_list_of_its_own() => _observedLists.Single().Single().Must().ReferenceEqual(_publishedTevent);
   }

   public class after_stopping_the_engine : Given_a_container_composing_a_LocalTessagingEngine
   {
      public after_stopping_the_engine() => ComposeContainerWithEngine(_ => {});

      protected override async Task InitializeAsyncInternal() => await Engine.StopAsync();

      [PCT] public void creating_a_dynamic_observer_throws_stating_that_the_engine_has_stopped() =>
         Invoking(() => DynamicTeventObservers.ObserveTevents(observe => observe.ForTevent((IMyNumberedTevent _) => {})))
            .Must().Throw<Exception>()
            .Which.Message.Must().Contain("has stopped");
   }
}
