using System.Transactions;
using Compze.DependencyInjection;
using Compze.Must;
using Compze.Tessaging.Typermedia;
using Compze.Tests.Integration.InProcess;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;
using static Compze.Must.MustActions;

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Engine;

///<summary>The asynchronous forms of the local navigators: <c>ExecuteAsync</c> on <see cref="IIndependentLocalTypermediaNavigator"/><br/>
/// and <see cref="ILocalTypermediaNavigatorSession"/>, beside each <c>Execute</c> (see the Typermedia partial).</summary>
public partial class Given_a_container_composing_a_LocalTessagingEngine
{
   public class with_a_declared_strictly_local_tuery_handler_and_tommand_handler_navigated_asynchronously : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly List<string> _registeredGreeters = [];

      public with_a_declared_strictly_local_tuery_handler_and_tommand_handler_navigated_asynchronously() =>
         ComposeContainerWithEngine(engine => engine.RegisterTypermediaHandlers(handle => handle
            .ForTuery((MyStrictlyLocalGreetingTuery tuery) => new MyGreeting { Message = $"Hello {tuery.Name}!" })
            .ForTommand((MyStrictlyLocalRegisterGreeterTommand tommand) => _registeredGreeters.Add(tommand.Name))
            .ForTommand((MyStrictlyLocalRegisterGreeterReturningTotalCountTommand tommand) =>
             {
                _registeredGreeters.Add(tommand.Name);
                return _registeredGreeters.Count;
             })));

      IIndependentLocalTypermediaNavigator Navigator => Container.RootResolver.Resolve<IIndependentLocalTypermediaNavigator>();

      [PCT] public async Task the_independent_navigator_returns_the_tuery_handlers_result() =>
         (await Navigator.ExecuteAsync(new MyStrictlyLocalGreetingTuery { Name = "World" })).Message.Must().Be("Hello World!");

      [PCT] public async Task the_independent_navigator_invokes_the_tommand_handler_in_its_own_unit_of_work()
      {
         await Navigator.ExecuteAsync(new MyStrictlyLocalRegisterGreeterTommand { Name = "Greta" });
         _registeredGreeters.Single().Must().Be("Greta");
      }

      [PCT] public async Task the_independent_navigator_returns_the_tommand_handlers_result() =>
         (await Navigator.ExecuteAsync(new MyStrictlyLocalRegisterGreeterReturningTotalCountTommand { Name = "Greta" })).Must().Be(1);

      [PCT] public async Task the_independent_navigator_fails_from_within_an_ambient_transaction() =>
         (await InvokingAsync(async () =>
                 {
                    using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                    await Navigator.ExecuteAsync(new MyStrictlyLocalGreetingTuery { Name = "World" });
                 })
                .Must().ThrowAsync<Exception>())
         .Which.Message.Must().Contain("ambient transaction");

      [PCT] public async Task the_navigator_session_returns_the_tuery_handlers_result_in_the_callers_scope() =>
         (await Container.ScopeFactory.ExecuteInIsolatedScopeAsync(async scope => await scope.Resolve<ILocalTypermediaNavigatorSession>().ExecuteAsync(new MyStrictlyLocalGreetingTuery { Name = "World" })))
        .Message.Must().Be("Hello World!");

      [PCT] public async Task the_navigator_session_invokes_the_tommand_handler_within_the_callers_unit_of_work()
      {
         await Container.ScopeFactory.ExecuteUnitOfWorkAsync(async unitOfWork => await unitOfWork.Resolve<ILocalTypermediaNavigatorSession>().ExecuteAsync(new MyStrictlyLocalRegisterGreeterTommand { Name = "Greta" }));
         _registeredGreeters.Single().Must().Be("Greta");
      }

      [PCT] public async Task the_navigator_session_returns_the_tommand_handlers_result_within_the_callers_unit_of_work() =>
         (await Container.ScopeFactory.ExecuteUnitOfWorkAsync(async unitOfWork => await unitOfWork.Resolve<ILocalTypermediaNavigatorSession>().ExecuteAsync(new MyStrictlyLocalRegisterGreeterReturningTotalCountTommand { Name = "Greta" })))
        .Must().Be(1);

      [PCT] public async Task the_navigator_session_fails_the_tommand_without_a_transaction() =>
         (await InvokingAsync(async () => await Container.ScopeFactory.ExecuteInIsolatedScopeAsync(async scope => await scope.Resolve<ILocalTypermediaNavigatorSession>().ExecuteAsync(new MyStrictlyLocalRegisterGreeterTommand { Name = "Greta" })))
                .Must().ThrowAsync<Exception>())
         .Which.Message.Must().Contain("but there is no transaction");
   }

   ///<summary>Each handler awaits a closed gate, holding no thread while it waits, so every navigation is started from the one thread<br/>
   /// of the specification: an <c>ExecuteAsync</c> that held its caller's thread while the handler awaits would never return to start<br/>
   /// the next.</summary>
   public class with_a_strictly_local_tommand_handler_that_awaits_a_closed_gate : Given_a_container_composing_a_LocalTessagingEngine
   {
      const int Navigations = 10;
      readonly IThreadGate _gate = IThreadGate.NewClosed(WaitTimeout.Seconds(10), nameof(_gate));

      public with_a_strictly_local_tommand_handler_that_awaits_a_closed_gate() =>
         ComposeContainerWithEngine(engine => engine.RegisterTypermediaHandlers(handle => handle
            .ForTommand(async (MyStrictlyLocalRegisterGreeterTommand _) => await _gate.AwaitPassThroughAsync())));

      public class after_ten_tommands_are_navigated_through_the_independent_navigator_from_one_thread : with_a_strictly_local_tommand_handler_that_awaits_a_closed_gate
      {
         readonly IReadOnlyList<Task> _navigations;

         public after_ten_tommands_are_navigated_through_the_independent_navigator_from_one_thread()
         {
            var navigator = Container.RootResolver.Resolve<IIndependentLocalTypermediaNavigator>();
            _navigations = [..Enumerable.Range(0, Navigations).Select(_ => navigator.ExecuteAsync(new MyStrictlyLocalRegisterGreeterTommand { Name = "Greta" }))];
         }

         [PCT] public void every_handler_waits_at_the_gate() => _gate.Queued.Must().Be(Navigations);

         [PCT] public void no_navigation_has_completed() => _navigations.Count(it => it.IsCompleted).Must().Be(0);

         [PCT] public async Task every_navigation_completes_once_the_gate_opens()
         {
            _gate.Open();
            await Task.WhenAll(_navigations);
            _gate.Passed.Must().Be(Navigations);
         }

         ///<summary>Lets the navigations that a specification left waiting at the gate complete before the container is disposed.</summary>
         protected override async Task DisposeAsyncInternal()
         {
            _gate.Open();
            await Task.WhenAll(_navigations);
            await base.DisposeAsyncInternal();
         }
      }
   }
}
