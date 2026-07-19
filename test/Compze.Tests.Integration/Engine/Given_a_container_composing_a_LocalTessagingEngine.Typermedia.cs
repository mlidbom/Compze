using System.Transactions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Runtime.Resolution;
using Compze.Must;

using Compze.Tessaging.Engine;
using Compze.Tessaging.Typermedia;
using Compze.Tests.Integration.InProcess;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Engine;

///<summary>The typermedia face of the engine (see the main partial): strictly-local tueries and tommands declared in the same<br/>
/// composition block execute synchronously, on the calling thread, in the caller's session — a tommand within the caller's transaction.</summary>
public partial class Given_a_container_composing_a_LocalTessagingEngine
{
   public class with_a_declared_strictly_local_tuery_handler_and_tommand_handler : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly List<string> _registeredGreeters = [];

      public with_a_declared_strictly_local_tuery_handler_and_tommand_handler() =>
         ComposeContainerWithEngine(engine => engine.RegisterTessageHandlers(handle => handle
            .ForTuery((MyStrictlyLocalGreetingTuery tuery) => new MyGreeting { Message = $"Hello {tuery.Name}!" })
            .ForTommand((MyStrictlyLocalRegisterGreeterTommand tommand) => _registeredGreeters.Add(tommand.Name))));

      [PCT] public void executing_the_tuery_through_the_unit_of_work_local_typermedia_navigator_returns_the_handlers_result() =>
         Container.ScopeFactory.ExecuteInIsolatedScope(scope => scope.Resolve<ILocalTypermediaNavigatorSession>().Execute(new MyStrictlyLocalGreetingTuery { Name = "World" }).Message.Must().Be("Hello World!"));

      [PCT] public void executing_the_tommand_through_the_unit_of_work_local_typermedia_navigator_within_a_unit_of_work_invokes_the_handler()
      {
         Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<ILocalTypermediaNavigatorSession>().Execute(new MyStrictlyLocalRegisterGreeterTommand { Name = "Greta" }));
         _registeredGreeters.Single().Must().Be("Greta");
      }

      [PCT] public void executing_the_tommand_without_a_transaction_fails_stating_the_transaction_policy() =>
         Container.ScopeFactory.ExecuteInIsolatedScope(scope =>
            Invoking(() => scope.Resolve<ILocalTypermediaNavigatorSession>().Execute(new MyStrictlyLocalRegisterGreeterTommand { Name = "Greta" }))
               .Must().Throw<Exception>().Which.Message.Must().Contain("but there is no transaction"));

      [PCT] public void executing_the_tuery_through_the_independent_local_typermedia_navigator_resolved_from_the_root_returns_the_handlers_result() =>
         Container.RootResolver.Resolve<IIndependentLocalTypermediaNavigator>().Execute(new MyStrictlyLocalGreetingTuery { Name = "World" }).Message.Must().Be("Hello World!");

      [PCT] public void executing_the_tommand_through_the_independent_local_typermedia_navigator_resolved_from_the_root_invokes_the_handler_in_its_own_unit_of_work()
      {
         Container.RootResolver.Resolve<IIndependentLocalTypermediaNavigator>().Execute(new MyStrictlyLocalRegisterGreeterTommand { Name = "Greta" });
         _registeredGreeters.Single().Must().Be("Greta");
      }

      [PCT] public void executing_through_the_independent_local_typermedia_navigator_from_within_an_ambient_transaction_throws() =>
         Invoking(() =>
                 {
                    using var transactionScope = new TransactionScope();
                    Container.RootResolver.Resolve<IIndependentLocalTypermediaNavigator>().Execute(new MyStrictlyLocalGreetingTuery { Name = "World" });
                 })
                .Must().Throw<Exception>()
                .Which.Message.Must().Contain("ambient transaction");
   }
}
