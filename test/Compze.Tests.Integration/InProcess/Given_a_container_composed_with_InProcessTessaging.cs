using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Must.Assertions;
using Compze.Tessaging;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tests.Infrastructure;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.InProcess;

///<summary>In-process Tessaging composed into a plain container — no endpoint, no host, no transport: the handler registry, the<br/>
/// synchronous in-process tevent delivery, and the container's one <see cref="ITeventPublisher"/>. The composition wires no remote<br/>
/// delivery legs, so every published tevent is delivered to this process's handlers only.</summary>
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

   public class after_publishing_an_unwrapped_tevent_through_the_tevent_publisher : Given_a_container_composed_with_InProcessTessaging
   {
      readonly List<IMyGreetingRequestedTevent> _receivedBySubscriberToTheTeventsBaseInterface = [];
      readonly List<IPublisherTevent<IMyGreetingRequestedTevent>> _receivedBySubscriberToTheWrapperOfTheTeventsBaseInterface = [];
      readonly List<IMyUnrelatedTevent> _receivedBySubscriberToAnUnrelatedTeventInterface = [];
      readonly MySpecialGreetingRequestedTevent _publishedTevent = new();
      readonly int _publishingThreadId;
      int _handlingThreadId;

      public after_publishing_an_unwrapped_tevent_through_the_tevent_publisher()
      {
         HandlerRegistrar.ForTevent<IMyGreetingRequestedTevent>((tevent, _) =>
         {
            _receivedBySubscriberToTheTeventsBaseInterface.Add(tevent);
            _handlingThreadId = Environment.CurrentManagedThreadId;
         });
         HandlerRegistrar.ForTevent<IPublisherTevent<IMyGreetingRequestedTevent>>((wrappedTevent, _) => _receivedBySubscriberToTheWrapperOfTheTeventsBaseInterface.Add(wrappedTevent));
         HandlerRegistrar.ForTevent<IMyUnrelatedTevent>((tevent, _) => _receivedBySubscriberToAnUnrelatedTeventInterface.Add(tevent));

         _publishingThreadId = Environment.CurrentManagedThreadId;
         Container.ScopeFactory.ExecuteInIsolatedScope(scope => scope.Resolve<ITeventPublisher>().Publish(_publishedTevent));
      }

      [PCT] public void the_subscriber_to_a_base_interface_of_the_published_tevents_type_receives_the_published_tevent() => _receivedBySubscriberToTheTeventsBaseInterface.Single().Must().ReferenceEqual(_publishedTevent);
      [PCT] public void the_subscriber_to_the_wrapper_of_that_base_interface_receives_the_auto_created_wrapper_wrapping_the_published_tevent() => _receivedBySubscriberToTheWrapperOfTheTeventsBaseInterface.Single().Tevent.Must().ReferenceEqual(_publishedTevent);
      [PCT] public void the_subscriber_runs_synchronously_on_the_publishing_thread() => _handlingThreadId.Must().Be(_publishingThreadId);
      [PCT] public void the_subscriber_to_an_unrelated_tevent_interface_receives_nothing() => _receivedBySubscriberToAnUnrelatedTeventInterface.Must().BeEmpty();
   }

   public class after_publishing_a_wrapped_taggregate_tevent_through_the_tevent_publisher : Given_a_container_composed_with_InProcessTessaging
   {
      readonly List<ITaggregateTevent> _receivedBySubscriberToTheInnerTeventType = [];
      readonly List<ITaggregateTevent<ITaggregateTevent>> _receivedBySubscriberToTheWrapperType = [];
      readonly TaggregateTevent<MyTaggregateTevent> _publishedWrappedTevent = new(new MyTaggregateTevent());

      public after_publishing_a_wrapped_taggregate_tevent_through_the_tevent_publisher()
      {
         HandlerRegistrar.ForTevent<ITaggregateTevent>((tevent, _) => _receivedBySubscriberToTheInnerTeventType.Add(tevent));
         HandlerRegistrar.ForTevent<ITaggregateTevent<ITaggregateTevent>>((wrappedTevent, _) => _receivedBySubscriberToTheWrapperType.Add(wrappedTevent));
         Container.ScopeFactory.ExecuteTransactionInIsolatedScope(scope => scope.Resolve<ITeventPublisher>().Publish(_publishedWrappedTevent));
      }

      [PCT] public void the_subscriber_to_the_inner_tevent_type_receives_the_inner_tevent() => _receivedBySubscriberToTheInnerTeventType.Single().Must().ReferenceEqual(_publishedWrappedTevent.Tevent);
      [PCT] public void the_subscriber_to_the_wrapper_type_receives_the_wrapper_itself() => _receivedBySubscriberToTheWrapperType.Single().Must().ReferenceEqual(_publishedWrappedTevent);
   }
}
