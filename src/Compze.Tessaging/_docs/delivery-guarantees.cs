// ReSharper disable All

using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.TypeIdentifiers.DependencyInjection;
using Compze.Tessaging.Documentation.DeclaringEndpoints;

#pragma warning disable // Documentation example code: deliberately illustrative, not production code.

namespace Compze.Tessaging.Documentation.DeliveryGuarantees;

#region declaring_handling_retries
class BillingEndpoint : ExactlyOnceEndpointDeclaration<BillingEndpoint>, IEndpointIdentity
{
   public static string Name => "Billing";
   public static EndpointId Id { get; } = new(Guid.Parse("4D7A2E91-B6C3-4F58-8A10-E39C5B72D6F4"));

   //A tessage whose handling keeps failing is tried for 30 minutes of failing, instead of the default 5, before it is marked failed.
   protected override HandlingRetries? HandlingRetries => HandlingRetries.For(TimeSpan.FromMinutes(30));

   protected override void RegisterComponents(IComponentRegistrar registrar) => registrar
      .RequireMappedTypesFromAssemblyContaining<RegisterAccount>();
}
#endregion
