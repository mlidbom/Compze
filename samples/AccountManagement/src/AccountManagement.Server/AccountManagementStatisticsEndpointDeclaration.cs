using AccountManagement.UI.QueryModels;
using Compze.DependencyInjection.Abstractions;
using Compze.DocumentDb.Wiring;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.TessageBus;

namespace AccountManagement;

///<summary>The AccountManagement statistics endpoint: query models maintained from the domain endpoint's tevents, stored as<br/>
/// documents. Every composition — production and tests alike — hosts this same declaration; the composition brings only the<br/>
/// <see cref="IEndpointEnvironment"/> it runs in.</summary>
public class AccountManagementStatisticsEndpointDeclaration : ExactlyOnceEndpointDeclaration<AccountManagementStatisticsEndpointDeclaration>, IEndpointIdentity
{
   public static string Name => "AccountManagementStatistics";
   public static EndpointId Id => new(Guid.Parse("B16250DE-4321-4FBD-A0CC-E42C7A1B0B34"));

   protected override void RegisterComponents(IComponentRegistrar registrar)
   {
      registrar.RequireAccountManagementTypeMappings();
      AccountStatistics.RegisterComponents(registrar);
   }

   protected override void RegisterExactlyOnceTeventHandlers(IExactlyOnceTeventHandlerRegistrar handle) =>
      AccountStatistics.MaintainStatisticsWhenRelevantTeventsAreReceived(handle);

   protected override void Declare(ExactlyOnceEndpointBuilder endpoint) =>
      endpoint.RegisterDocumentDb()
              .HandleDocumentType<AccountStatistics.SingletonStatisticsQueryModel>();
}
