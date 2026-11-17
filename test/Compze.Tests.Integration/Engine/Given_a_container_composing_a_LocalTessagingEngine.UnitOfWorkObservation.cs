using System.Collections.Concurrent;
using Compze.Must;
using Compze.Tessaging.TessageBus;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Integration.InProcess;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Engine;

///<summary>Observers of each committed unit of work's tevents as one list, declared with <c>ForEachUnitOfWork</c>, at composition<br/>
/// and at runtime.</summary>
public partial class Given_a_container_composing_a_LocalTessagingEngine
{
   public class with_an_observer_of_each_unit_of_works_numbered_tevents_declared_at_composition : Given_a_container_composing_a_LocalTessagingEngine
   {
      ConcurrentQueue<int[]> ObservedLists { get; } = [];

      protected with_an_observer_of_each_unit_of_works_numbered_tevents_declared_at_composition() =>
         ComposeContainerWithEngine(engine => engine.ObserveTevents(observe => observe.ForEachUnitOfWork((IReadOnlyList<IMyNumberedTevent> tevents) =>
                                                                                                          ObservedLists.Enqueue([..tevents.Select(it => it.SequenceNumber)]))));

      protected override async Task InitializeAsyncInternal() => await Engine.AwaitNoTessagesInFlightAsync();

      public class after_one_unit_of_work_publishes_tevents_1_2_and_3 : with_an_observer_of_each_unit_of_works_numbered_tevents_declared_at_composition
      {
         public after_one_unit_of_work_publishes_tevents_1_2_and_3() => PublishInAUnitOfWork(new MyNumberedTevent(1), new MyNumberedTevent(2), new MyNumberedTevent(3));

         [PCT] public void it_observes_them_as_one_list_in_publish_order() => ObservedLists.ToArray().Must().DeepEqual([[1, 2, 3]]);
      }

      public class after_one_unit_of_work_publishes_tevent_1_a_greeting_and_tevent_2 : with_an_observer_of_each_unit_of_works_numbered_tevents_declared_at_composition
      {
         public after_one_unit_of_work_publishes_tevent_1_a_greeting_and_tevent_2() => PublishInAUnitOfWork(new MyNumberedTevent(1), new MySpecialGreetingRequestedTevent(), new MyNumberedTevent(2));

         [PCT] public void the_list_holds_only_the_numbered_tevents() => ObservedLists.ToArray().Must().DeepEqual([[1, 2]]);
      }

      public class after_one_unit_of_work_publishes_only_a_greeting : with_an_observer_of_each_unit_of_works_numbered_tevents_declared_at_composition
      {
         public after_one_unit_of_work_publishes_only_a_greeting() => PublishInAUnitOfWork(new MySpecialGreetingRequestedTevent());

         [PCT] public void it_is_not_called() => ObservedLists.Must().BeEmpty();
      }

      public class after_one_unit_of_work_publishes_tevents_1_and_2_and_the_next_publishes_tevent_3 : with_an_observer_of_each_unit_of_works_numbered_tevents_declared_at_composition
      {
         public after_one_unit_of_work_publishes_tevents_1_and_2_and_the_next_publishes_tevent_3()
         {
            PublishInAUnitOfWork(new MyNumberedTevent(1), new MyNumberedTevent(2));
            PublishInAUnitOfWork(new MyNumberedTevent(3));
         }

         [PCT] public void it_observes_two_lists_one_for_each_unit_of_work() => ObservedLists.ToArray().Must().DeepEqual([[1, 2], [3]]);
      }
   }

   public class with_an_observer_of_each_unit_of_works_numbered_tevents_created_at_runtime : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly ConcurrentQueue<int[]> _observedLists = [];

      public with_an_observer_of_each_unit_of_works_numbered_tevents_created_at_runtime()
      {
         ComposeContainerWithEngine(engine => engine.ObserveTevents(observe => observe.AtRuntime<IMyNumberedTevent>()));
         TeventObservation.ObserveTevents(observe => observe.ForEachUnitOfWork((IReadOnlyList<IMyNumberedTevent> tevents) =>
                                                                                  _observedLists.Enqueue([..tevents.Select(it => it.SequenceNumber)])));
         PublishInAUnitOfWork(new MyNumberedTevent(1), new MyNumberedTevent(2));
         PublishInAUnitOfWork(new MyNumberedTevent(3));
      }

      protected override async Task InitializeAsyncInternal() => await Engine.AwaitNoTessagesInFlightAsync();

      [PCT] public void it_observes_each_unit_of_works_tevents_as_one_list() => _observedLists.ToArray().Must().DeepEqual([[1, 2], [3]]);
   }
}
