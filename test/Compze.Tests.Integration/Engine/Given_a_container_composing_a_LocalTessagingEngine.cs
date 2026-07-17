using System.Transactions;
using Compze.Abstractions.Tessaging.Public;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE.Testing;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;

using Compze.Tessaging.Engine;
using Compze.Tests.Infrastructure;
using Compze.Tests.Integration.InProcess;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Engine;

///<summary>The LocalTessagingEngine composed into a plain container — no endpoint, no host, no transport: the tessage-conversing<br/>
/// heart of one container, declared in one composition block, wiring no remote delivery legs — so every published tevent is<br/>
/// delivered to this process's handlers only, synchronously, within the publisher's transaction, and strictly-local typermedia<br/>
/// executes on the calling thread in the caller's session (the typermedia contexts live in the Typermedia partial).</summary>
public partial class Given_a_container_composing_a_LocalTessagingEngine : UniversalTestBase
{
   IDependencyInjectionContainer? _container;

   protected IDependencyInjectionContainer Container
   {
      get
      {
         State.Assert(_container != null, () => $"Compose the context's container first: call {nameof(ComposeContainerWithEngine)} in the nested context's constructor.");
         return _container;
      }
   }

   ///<summary>Each nested context composes its own container: the engine's declaration block is the one and only way handlers get<br/>
   /// into the engine, so a context's handlers are declared at composition — never registered afterward.</summary>
   protected void ComposeContainerWithEngine(Action<LocalTessagingEngineBuilder> engine)
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.LocalTessagingEngine(engine);
      _container = builder.Build();
   }

   protected override async Task DisposeAsyncInternal()
   {
      if(_container != null) await _container.DisposeAsync();
   }

   public class after_publishing_an_unwrapped_tevent_through_the_unit_of_work_tevent_publisher : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly List<IMyGreetingRequestedTevent> _receivedBySubscriberToTheTeventsBaseInterface = [];
      readonly List<IPublisherTevent<IMyGreetingRequestedTevent>> _receivedBySubscriberToTheWrapperOfTheTeventsBaseInterface = [];
      readonly List<IMyUnrelatedTevent> _receivedBySubscriberToAnUnrelatedTeventInterface = [];
      readonly MySpecialGreetingRequestedTevent _publishedTevent = new();
      readonly int _publishingThreadId;
      int _handlingThreadId;

      public after_publishing_an_unwrapped_tevent_through_the_unit_of_work_tevent_publisher()
      {
         ComposeContainerWithEngine(engine => engine.RegisterTessageHandlers(handle => handle
            .ForTevent((IMyGreetingRequestedTevent tevent) =>
             {
                _receivedBySubscriberToTheTeventsBaseInterface.Add(tevent);
                _handlingThreadId = Environment.CurrentManagedThreadId;
             })
            .ForTevent((IPublisherTevent<IMyGreetingRequestedTevent> wrappedTevent) => _receivedBySubscriberToTheWrapperOfTheTeventsBaseInterface.Add(wrappedTevent))
            .ForTevent((IMyUnrelatedTevent tevent) => _receivedBySubscriberToAnUnrelatedTeventInterface.Add(tevent))));

         _publishingThreadId = Environment.CurrentManagedThreadId;
         Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(_publishedTevent));
      }

      [PCT] public void the_subscriber_to_a_base_interface_of_the_published_tevents_type_receives_the_published_tevent() => _receivedBySubscriberToTheTeventsBaseInterface.Single().Must().ReferenceEqual(_publishedTevent);
      [PCT] public void the_subscriber_to_the_wrapper_of_that_base_interface_receives_the_auto_created_wrapper_wrapping_the_published_tevent() => _receivedBySubscriberToTheWrapperOfTheTeventsBaseInterface.Single().Tevent.Must().ReferenceEqual(_publishedTevent);
      [PCT] public void the_subscriber_runs_synchronously_on_the_publishing_thread() => _handlingThreadId.Must().Be(_publishingThreadId);
      [PCT] public void the_subscriber_to_an_unrelated_tevent_interface_receives_nothing() => _receivedBySubscriberToAnUnrelatedTeventInterface.Must().BeEmpty();
   }

   public class after_publishing_a_wrapped_taggregate_tevent_through_the_unit_of_work_tevent_publisher : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly List<ITaggregateTevent> _receivedBySubscriberToTheInnerTeventType = [];
      readonly List<ITaggregateTevent<ITaggregateTevent>> _receivedBySubscriberToTheWrapperType = [];
      readonly TaggregateTevent<MyTaggregateTevent> _publishedWrappedTevent = new(new MyTaggregateTevent());

      public after_publishing_a_wrapped_taggregate_tevent_through_the_unit_of_work_tevent_publisher()
      {
         //Taggregate tevents are exactly-once kinds, and exactly-once kinds are async end to end - so these subscriptions declare async handlers.
         ComposeContainerWithEngine(engine => engine.RegisterTessageHandlers(handle => handle
            .ForTevent((ITaggregateTevent tevent) =>
             {
                _receivedBySubscriberToTheInnerTeventType.Add(tevent);
                return Task.CompletedTask;
             })
            .ForTevent((ITaggregateTevent<ITaggregateTevent> wrappedTevent) =>
             {
                _receivedBySubscriberToTheWrapperType.Add(wrappedTevent);
                return Task.CompletedTask;
             })));

         Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(_publishedWrappedTevent));
      }

      [PCT] public void the_subscriber_to_the_inner_tevent_type_receives_the_inner_tevent() => _receivedBySubscriberToTheInnerTeventType.Single().Must().ReferenceEqual(_publishedWrappedTevent.Tevent);
      [PCT] public void the_subscriber_to_the_wrapper_type_receives_the_wrapper_itself() => _receivedBySubscriberToTheWrapperType.Single().Must().ReferenceEqual(_publishedWrappedTevent);
   }

   public class after_publishing_a_tevent_inside_a_transaction_that_rolls_back : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly List<IMyGreetingRequestedTevent> _receivedBySubscriber = [];
      readonly List<IMyGreetingRequestedTevent> _observedByObserver = [];
      readonly MySpecialGreetingRequestedTevent _publishedTevent = new();

      public after_publishing_a_tevent_inside_a_transaction_that_rolls_back()
      {
         ComposeContainerWithEngine(engine => engine
            .RegisterTessageHandlers(handle => handle.ForTevent((IMyGreetingRequestedTevent tevent) => _receivedBySubscriber.Add(tevent)))
            .ObserveTevents(observe => observe.ForTevent((IMyGreetingRequestedTevent tevent) => _observedByObserver.Add(tevent))));

         Invoking(() => Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork =>
                       {
                          Transaction.Current!.FailOnPrepare();
                          unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(_publishedTevent);
                       }))
                      .Must().Throw<TransactionAbortedException>();
      }

      [PCT] public void the_subscriber_received_the_tevent_inside_the_doomed_transaction() => _receivedBySubscriber.Single().Must().ReferenceEqual(_publishedTevent);
      [PCT] public void the_observer_observed_the_tevent_despite_the_rollback() => _observedByObserver.Single().Must().ReferenceEqual(_publishedTevent);
   }

   public class after_publishing_a_tevent_when_the_first_of_two_observers_throws : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly List<IMyGreetingRequestedTevent> _receivedBySubscriber = [];
      readonly List<IMyGreetingRequestedTevent> _observedBySecondObserver = [];
      readonly MySpecialGreetingRequestedTevent _publishedTevent = new();

      public after_publishing_a_tevent_when_the_first_of_two_observers_throws()
      {
         ComposeContainerWithEngine(engine => engine
            .RegisterTessageHandlers(handle => handle.ForTevent((IMyGreetingRequestedTevent tevent) => _receivedBySubscriber.Add(tevent)))
            .ObserveTevents(observe => observe
               .ForTevent((IMyGreetingRequestedTevent _) => throw new Exception("thrown by the first observer"))
               .ForTevent((IMyGreetingRequestedTevent tevent) => _observedBySecondObserver.Add(tevent))));

         Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(_publishedTevent));
      }

      [PCT] public void the_publish_completes_and_the_subscriber_receives_the_tevent() => _receivedBySubscriber.Single().Must().ReferenceEqual(_publishedTevent);
      [PCT] public void the_second_observer_still_observes_the_tevent() => _observedBySecondObserver.Single().Must().ReferenceEqual(_publishedTevent);
   }

   public class after_publishing_a_tevent_through_the_independent_tevent_publisher_resolved_from_the_root : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly List<IMyGreetingRequestedTevent> _receivedBySubscriber = [];
      readonly MySpecialGreetingRequestedTevent _publishedTevent = new();

      public after_publishing_a_tevent_through_the_independent_tevent_publisher_resolved_from_the_root()
      {
         ComposeContainerWithEngine(engine => engine.RegisterTessageHandlers(handle => handle.ForTevent((IMyGreetingRequestedTevent tevent) => _receivedBySubscriber.Add(tevent))));
         Container.RootResolver.Resolve<IIndependentTeventPublisher>().Publish(_publishedTevent);
      }

      [PCT] public void the_subscriber_receives_the_published_tevent() => _receivedBySubscriber.Single().Must().ReferenceEqual(_publishedTevent);
   }

   public class the_independent_tevent_publisher : Given_a_container_composing_a_LocalTessagingEngine
   {
      public the_independent_tevent_publisher() => ComposeContainerWithEngine(engine => {});

      [PCT] public void throws_when_called_from_within_an_ambient_transaction() =>
         Invoking(() =>
                 {
                    using var transactionScope = new TransactionScope();
                    Container.RootResolver.Resolve<IIndependentTeventPublisher>().Publish(new MySpecialGreetingRequestedTevent());
                 })
                .Must().Throw<Exception>()
                .Which.Message.Must().Contain("ambient transaction");
   }

   public class the_unit_of_work_tevent_publisher : Given_a_container_composing_a_LocalTessagingEngine
   {
      public the_unit_of_work_tevent_publisher() => ComposeContainerWithEngine(engine => {});

      [PCT] public void throws_when_publishing_outside_an_ambient_transaction() =>
         Container.ScopeFactory.ExecuteInIsolatedScope(scope =>
            Invoking(() => scope.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MySpecialGreetingRequestedTevent()))
                          .Must().Throw<Exception>()
                          .Which.Message.Must().Contain("ambient transaction"));
   }

   public class composing_a_second_engine_into_the_same_container : Given_a_container_composing_a_LocalTessagingEngine
   {
      [PCT] public void explodes_stating_exactly_one_engine_per_container()
      {
         var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
         builder.Registrar.LocalTessagingEngine(engine => {});
         Invoking(() => builder.Registrar.LocalTessagingEngine(engine => {}))
            .Must().Throw<Exception>()
            .Which.Message.Must().Contain("exactly one engine per container");
      }
   }

   public class a_registrar_held_beyond_its_declaration_callback : Given_a_container_composing_a_LocalTessagingEngine
   {
      TessageHandlerRegistrar _escapedRegistrar = null!;

      public a_registrar_held_beyond_its_declaration_callback() =>
         ComposeContainerWithEngine(engine => engine.RegisterTessageHandlers(handle => _escapedRegistrar = handle));

      [PCT] public void explodes_when_used_stating_that_the_registrar_exists_only_inside_its_callback() =>
         Invoking(() => _escapedRegistrar.ForTevent((IMyGreetingRequestedTevent _) => {}))
            .Must().Throw<Exception>()
            .Which.Message.Must().Contain("the registrar exists only inside it");
   }
}
