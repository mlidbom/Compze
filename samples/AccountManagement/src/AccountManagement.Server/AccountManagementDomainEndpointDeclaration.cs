using AccountManagement.Domain;
using AccountManagement.Domain.Tevents;
using AccountManagement.UI;
using AccountManagement.UI.QueryModels;
using Compze.DependencyInjection.Abstractions;
using Compze.DocumentDb.Wiring;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Typermedia;
using Compze.Teventive.TeventStore.Typermedia;

namespace AccountManagement;

///<summary>The AccountManagement domain endpoint: the <see cref="Account"/> taggregate's tevent store, the email-to-account<br/>
/// mapping documents, and the domain's typermedia and tessage-bus handlers. Every composition — production and tests alike —<br/>
/// hosts this same declaration; the composition brings only the <see cref="IEndpointEnvironment"/> it runs in.</summary>
public class AccountManagementDomainEndpointDeclaration : ExactlyOnceEndpointDeclaration<AccountManagementDomainEndpointDeclaration>, IEndpointIdentity
{
   public static string Name => "AccountManagement";
   public static EndpointId Id => new(Guid.Parse("1A1BE9C8-C8F6-4E38-ABFB-F101E5EDB00D"));

   //The statistics endpoint's query models update from this endpoint's tevents. Requiring the peer makes the bus queue its
   //tevents for it even before first contact, so traffic can open without awaiting discovery.
   protected override IReadOnlyList<EndpointId> RequiredPeers => [AccountManagementStatisticsEndpointDeclaration.Id];

   protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireAccountManagementTypeMappings();

   protected override void RegisterExactlyOnceTeventHandlers(IExactlyOnceTeventHandlerRegistrar handle) =>
      EmailToAccountMapper.UpdateMappingWhenEmailChanges(handle);

   protected override void RegisterTypermediaTommandHandlers(ITypermediaTommandHandlerRegistrar handle) =>
      UIAdapterLayer.RegisterTommandHandlers(handle);

   protected override void RegisterTueryHandlers(ITueryHandlerRegistrar handle)
   {
      UIAdapterLayer.RegisterTueryHandlers(handle);
      AccountQueryModel.Api.RegisterHandlers(handle);
      EmailToAccountMapper.TryGetAccountByEmail(handle);
   }

   //The stores this endpoint's domain database holds: the Account taggregate's tevent store, and the email-to-account
   //mapping documents.
   protected override void Declare(ExactlyOnceEndpointBuilder endpoint)
   {
      endpoint.RegisterTeventStore()
              .HandleTaggregate<Account, IAccountTevent>();

      endpoint.RegisterDocumentDb()
              .HandleDocumentType<TeventStoreApi.TueryApi.TaggregateLink<Account>>();
   }
}
