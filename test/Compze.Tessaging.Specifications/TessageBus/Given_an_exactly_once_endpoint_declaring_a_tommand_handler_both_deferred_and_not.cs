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

///<summary>A tommand type has one handler, deferred or not: an endpoint declaring both for <see cref="DeferredStep"/> fails to build.</summary>
public class Given_an_exactly_once_endpoint_declaring_a_tommand_handler_both_deferred_and_not : UniversalTestBase
{
   readonly TestingEndpointHost _host = TestingEndpointHost.Create();

   [PCT] public void registering_the_endpoint_explodes_saying_a_handler_is_already_registered() =>
      Invoking(() => _host.RegisterEndpoint(new TheEndpoint())).Must().Throw<InvalidOperationException>().Which.Message.Must().Contain("is already registered");

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   class TheEndpoint : ExactlyOnceEndpointDeclaration<TheEndpoint>, IEndpointIdentity
   {
      public static string Name => "DeferredAndNot";
      public static EndpointId Id => new(Guid.Parse("6F8A2C15-9D47-4B30-8E61-C4B72D9F0A38"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingSpecificationTypeMappings();

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((DeferredStep _, IUnitOfWorkResolver _) => Task.CompletedTask);

      protected override void RegisterDeferredTommandHandlers(IDeferredTommandHandlerRegistrar handle) => handle
         .ForTommand((DeferredStep _, IUnitOfWorkResolver _) => Task.CompletedTask);
   }
}
