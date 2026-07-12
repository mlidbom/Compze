using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Teventive.TeventStore.Internal;
using Compze.Tests.Infrastructure;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.InProcess;

///<summary>In-process Tessaging composed into a plain container — no endpoint, no host, no transport: the handler registry, synchronous in-process tevent delivery, and the in-process-only tevent publication mode.</summary>
public class Given_a_container_composed_with_InProcessTessaging : UniversalTestBase
{
   protected IDependencyInjectionContainer Container { get; }
   protected ITessageHandlerRegistrar HandlerRegistrar { get; }

   public Given_a_container_composed_with_InProcessTessaging()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.InProcessTessaging();
      Container = builder.Build();
      HandlerRegistrar = Container.RootResolver.Resolve<ITessageHandlerRegistrar>();
   }

   protected override async Task DisposeAsyncInternal() => await Container.DisposeAsync();

   public class after_publishing_a_tevent_through_the_in_process_tevent_publisher : Given_a_container_composed_with_InProcessTessaging
   {
      readonly List<IMyGreetingRequestedTevent> _receivedBySubscriberToTheTeventsBaseInterface = [];
      readonly List<IMyUnrelatedTevent> _receivedBySubscriberToAnUnrelatedTeventInterface = [];
      readonly int _publishingThreadId;
      int _handlingThreadId;

      public after_publishing_a_tevent_through_the_in_process_tevent_publisher()
      {
         HandlerRegistrar.ForTevent<IMyGreetingRequestedTevent>((tevent, _) =>
         {
            _receivedBySubscriberToTheTeventsBaseInterface.Add(tevent);
            _handlingThreadId = Environment.CurrentManagedThreadId;
         });
         HandlerRegistrar.ForTevent<IMyUnrelatedTevent>((tevent, _) => _receivedBySubscriberToAnUnrelatedTeventInterface.Add(tevent));

         _publishingThreadId = Environment.CurrentManagedThreadId;
         Container.ScopeFactory.ExecuteInIsolatedScope(scope => scope.Resolve<IInProcessTeventPublisher>().Publish(new MySpecialGreetingRequestedTevent(), scope));
      }

      [PCT] public void the_subscriber_to_a_base_interface_of_the_published_tevents_type_receives_it() => _receivedBySubscriberToTheTeventsBaseInterface.Must().HaveCount(1);
      [PCT] public void the_subscriber_runs_synchronously_on_the_publishing_thread() => _handlingThreadId.Must().Be(_publishingThreadId);
      [PCT] public void the_subscriber_to_an_unrelated_tevent_interface_receives_nothing() => _receivedBySubscriberToAnUnrelatedTeventInterface.Must().BeEmpty();
   }

   public class after_publishing_a_taggregate_tevent_through_the_tevent_store_tevent_publisher : Given_a_container_composed_with_InProcessTessaging
   {
      readonly List<ITaggregateTevent> _receivedBySubscriberInThisProcess = [];

      public after_publishing_a_taggregate_tevent_through_the_tevent_store_tevent_publisher()
      {
         HandlerRegistrar.ForTevent<ITaggregateTevent>((tevent, _) => _receivedBySubscriberInThisProcess.Add(tevent));
         Container.ScopeFactory.ExecuteTransactionInIsolatedScope(scope => scope.Resolve<ITeventStoreTeventPublisher>().Publish(new MyTaggregateTevent(), scope));
      }

      [PCT] public void the_subscriber_in_this_process_receives_it() => _receivedBySubscriberInThisProcess.Must().HaveCount(1);
   }
}
