using System.Transactions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;

using Compze.Tests.Infrastructure;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Typermedia.HandlerRegistration;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.InProcess;

///<summary>In-process Typermedia composed into a plain container — no endpoint, no host, no transport server, no discovery: the handler registry and the in-process navigator through which strictly local tueries and tommands execute synchronously.</summary>
public class Given_a_container_composed_with_InProcessTypermedia : UniversalTestBase
{
   protected IDependencyInjectionContainer Container { get; }
   protected TypermediaHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

   public Given_a_container_composed_with_InProcessTypermedia()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.InProcessTypermedia();
      Container = builder.Build();
      RegisterHandlers = new TypermediaHandlerRegistrarWithDependencyInjectionSupport(Container.RootResolver.Resolve<ITypermediaHandlerRegistrar>());
   }

   protected override async Task DisposeAsyncInternal() => await Container.DisposeAsync();

   public class after_registering_a_strictly_local_tuery_handler_and_a_strictly_local_tommand_handler : Given_a_container_composed_with_InProcessTypermedia
   {
      readonly List<string> _registeredGreeters = [];

      public after_registering_a_strictly_local_tuery_handler_and_a_strictly_local_tommand_handler()
      {
         RegisterHandlers.ForTuery((MyStrictlyLocalGreetingTuery tuery) => new MyGreeting { Message = $"Hello {tuery.Name}!" })
                         .ForTommand((MyStrictlyLocalRegisterGreeterTommand tommand) => _registeredGreeters.Add(tommand.Name));
      }

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
