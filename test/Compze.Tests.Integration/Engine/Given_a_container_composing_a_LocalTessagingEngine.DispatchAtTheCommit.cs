using System.Transactions;
using Compze.DependencyInjection;
using Compze.Must;
using Compze.Tessaging.TessageBus;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Integration.InProcess;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Engine;

///<summary>The contexts of when a tevent published through the <see cref="IUnitOfWorkTeventPublisher"/> is dispatched: when the<br/>
/// transaction of its unit of work prepares to commit, not when it is published.</summary>
public partial class Given_a_container_composing_a_LocalTessagingEngine
{
   public class after_a_unit_of_work_publishes_a_tevent_and_its_body_goes_on : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly List<string> _whatHappened = [];
      string? _theTransactionOfTheUnitOfWork;
      string? _theTransactionTheParticipationHandlerRanIn;

      public after_a_unit_of_work_publishes_a_tevent_and_its_body_goes_on()
      {
         ComposeContainerWithEngine(engine => engine.RegisterTessageBusHandlers(handle => handle.ForTevent((IMyGreetingRequestedTevent _) =>
         {
            _whatHappened.Add("the participation handler ran");
            _theTransactionTheParticipationHandlerRanIn = Transaction.Current?.TransactionInformation.LocalIdentifier;
         })));

         Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork =>
         {
            _theTransactionOfTheUnitOfWork = Transaction.Current!.TransactionInformation.LocalIdentifier;
            unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MySpecialGreetingRequestedTevent());
            _whatHappened.Add("the publish returned");
            _whatHappened.Add("the body of the unit of work returned");
         });
      }

      [PCT] public void the_participation_handler_runs_once_the_body_of_the_unit_of_work_has_returned() =>
         _whatHappened.Must().SequenceEqual(["the publish returned", "the body of the unit of work returned", "the participation handler ran"]);

      [PCT] public void the_participation_handler_runs_inside_the_transaction_of_the_unit_of_work() =>
         _theTransactionTheParticipationHandlerRanIn.Must().Be(_theTransactionOfTheUnitOfWork);
   }

   public class after_a_nested_unit_of_work_publishes_a_tevent : Given_a_container_composing_a_LocalTessagingEngine
   {
      ScopeMarker? _markerOfTheUnitOfWorkThatBeganTheTransaction;
      ScopeMarker? _markerOfTheNestedUnitOfWork;
      ScopeMarker? _markerTheParticipationHandlerGot;

      public after_a_nested_unit_of_work_publishes_a_tevent()
      {
         ComposeContainerWithEngine(components => components.Register(Scoped.For<ScopeMarker>().CreatedBy(() => new ScopeMarker())),
                                    engine => engine.RegisterTessageBusHandlers(handle => handle.ForTevent((IMyGreetingRequestedTevent _, ScopeMarker marker) => _markerTheParticipationHandlerGot = marker)));

         Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork =>
         {
            _markerOfTheUnitOfWorkThatBeganTheTransaction = unitOfWork.Resolve<ScopeMarker>();
            Container.ScopeFactory.ExecuteUnitOfWork(nestedUnitOfWork =>
            {
               _markerOfTheNestedUnitOfWork = nestedUnitOfWork.Resolve<ScopeMarker>();
               nestedUnitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MySpecialGreetingRequestedTevent());
            });
         });
      }

      [PCT] public void the_nested_unit_of_work_has_a_scope_of_its_own() => _markerOfTheNestedUnitOfWork.Must().NotReferenceEqual(_markerOfTheUnitOfWorkThatBeganTheTransaction);

      [PCT] public void the_participation_handler_runs_in_the_scope_of_the_unit_of_work_that_began_the_transaction() =>
         _markerTheParticipationHandlerGot.Must().ReferenceEqual(_markerOfTheUnitOfWorkThatBeganTheTransaction);

      ///<summary>A component of which each scope has an instance of its own.</summary>
      class ScopeMarker;
   }

   public class after_a_participation_handler_throws_when_the_unit_of_work_commits : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly InvalidOperationException _thrownByTheParticipationHandler = new("thrown by the participation handler");
      readonly List<IMyGreetingRequestedTevent> _observed = [];
      readonly Exception _exception;
      bool _thePublishReturned;

      public after_a_participation_handler_throws_when_the_unit_of_work_commits()
      {
         ComposeContainerWithEngine(engine => engine
            .RegisterTessageBusHandlers(handle => handle.ForTevent((IMyGreetingRequestedTevent _) => throw _thrownByTheParticipationHandler))
            .ObserveTevents(observe => observe.ForTevent((IMyGreetingRequestedTevent tevent) => _observed.Add(tevent))));

         _exception = Invoking(() => Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork =>
                                {
                                   unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MySpecialGreetingRequestedTevent());
                                   _thePublishReturned = true;
                                }))
                     .Must().Throw<TransactionAbortedException>().Which;
      }

      [PCT] public void the_publish_returns_without_throwing() => _thePublishReturned.Must().BeTrue();
      [PCT] public void the_exception_inside_the_transaction_aborted_exception_names_the_tevent() => _exception.InnerException!.Message.Must().Contain(nameof(MySpecialGreetingRequestedTevent));
      [PCT] public void the_exception_naming_the_tevent_holds_the_exception_of_the_participation_handler() => _exception.InnerException!.InnerException.Must().ReferenceEqual(_thrownByTheParticipationHandler);
      //Deterministic without any waiting: a tevent is queued for its observers only when its unit of work commits, and this one never did.
      [PCT] public void the_observer_observes_nothing() => _observed.Must().BeEmpty();
   }

   public class after_a_unit_of_work_publishes_a_tevent_in_a_transaction_that_ExecuteUnitOfWork_did_not_begin : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly List<IMyGreetingRequestedTevent> _receivedByTheParticipationHandler = [];
      readonly Exception _exception;

      public after_a_unit_of_work_publishes_a_tevent_in_a_transaction_that_ExecuteUnitOfWork_did_not_begin()
      {
         ComposeContainerWithEngine(engine => engine.RegisterTessageBusHandlers(handle => handle.ForTevent((IMyGreetingRequestedTevent tevent) => _receivedByTheParticipationHandler.Add(tevent))));

         _exception = Invoking(() =>
                              {
                                 using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                                 Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MySpecialGreetingRequestedTevent()));
                                 transactionScope.Complete();
                              })
                     .Must().Throw<Exception>().Which;
      }

      [PCT] public void the_publish_throws_saying_to_begin_the_transaction_with_ExecuteUnitOfWork() => _exception.Message.Must().Contain("begin the transaction with ExecuteUnitOfWork");
      [PCT] public void the_participation_handler_receives_nothing() => _receivedByTheParticipationHandler.Must().BeEmpty();
   }
}
