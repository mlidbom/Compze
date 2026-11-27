using System.Transactions;
using Compze.DependencyInjection.Abstractions;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.TessageBus;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.TessageBus;

///<summary>A handler's registration may choose a transaction timeout longer than zero and no longer than<br/>
/// <see cref="TransactionManager.MaximumTimeout"/>, which would otherwise silently shorten it: an endpoint declaring another fails<br/>
/// to build.</summary>
public abstract class Given_an_exactly_once_endpoint_declaring_a_handler_with_a_transaction_timeout_it_cannot_have : UniversalTestBase
{
   readonly TestingEndpointHost _host = TestingEndpointHost.Create();

   protected abstract TimeSpan TheTransactionTimeout { get; }

   protected string WhyRegisteringTheEndpointExploded() =>
      Invoking(() => _host.RegisterEndpoint(new TheEndpoint(TheTransactionTimeout))).Must().Throw<ArgumentException>().Which.Message;

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   public class that_is_longer_than_the_transaction_managers_maximum : Given_an_exactly_once_endpoint_declaring_a_handler_with_a_transaction_timeout_it_cannot_have
   {
      protected override TimeSpan TheTransactionTimeout => TransactionManager.MaximumTimeout + TimeSpan.FromSeconds(1);

      [PCT] public void registering_the_endpoint_explodes_naming_the_maximum() => WhyRegisteringTheEndpointExploded().Must().Contain("TransactionManager.MaximumTimeout");
   }

   public class of_zero : Given_an_exactly_once_endpoint_declaring_a_handler_with_a_transaction_timeout_it_cannot_have
   {
      protected override TimeSpan TheTransactionTimeout => TimeSpan.Zero;

      [PCT] public void registering_the_endpoint_explodes_saying_it_must_be_longer_than_zero() => WhyRegisteringTheEndpointExploded().Must().Contain("longer than zero");
   }

   class TheEndpoint(TimeSpan transactionTimeout) : ExactlyOnceEndpointDeclaration<TheEndpoint>, IEndpointIdentity
   {
      readonly TimeSpan _transactionTimeout = transactionTimeout;

      public static string Name => "TransactionTimeoutItCannotHave";
      public static EndpointId Id => new(Guid.Parse("5A0C8E26-91D4-4B73-A6F2-E38B17C94D05"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingSpecificationTypeMappings();

      protected override void RegisterDeferredTommandHandlers(IDeferredTommandHandlerRegistrar handle) => handle
         .ForTommand((StepWithATransactionTimeoutOfASecond _, IUnitOfWorkResolver _) => Task.CompletedTask, transactionTimeout: _transactionTimeout);
   }
}
