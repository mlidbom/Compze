using AccountManagement.Domain;
using AccountManagement.Domain.Tevents;
using AccountManagement.UI;
using AccountManagement.UI.QueryModels;
using Compze.Tessaging.Endpoints;
using Compze.DocumentDb.Wiring;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Teventive.TeventStore.Typermedia;

namespace AccountManagement;

///<summary>Declares the AccountManagement domain's two endpoints — what they handle and store. The composition hosting them<br/>
/// declares the environment: container technology, transport, serializer, domain database and discovery. The endpoint names<br/>
/// and ids are declared here so that every composition — production and tests alike — hosts the same endpoints.</summary>
public static class AccountManagementServerDomainBootstrapper
{
   public const string DomainEndpointName = "AccountManagement";
   public static readonly EndpointId DomainEndpointId = new(Guid.Parse("1A1BE9C8-C8F6-4E38-ABFB-F101E5EDB00D"));

   public const string StatisticsEndpointName = "AccountManagementStatistics";
   public static readonly EndpointId StatisticsEndpointId = new(Guid.Parse("B16250DE-4321-4FBD-A0CC-E42C7A1B0B34"));

   ///<summary>The domain endpoint: the <see cref="Account"/> taggregate's tevent store, the email-to-account mapping documents,<br/>
   /// and the domain's typermedia and tessage-bus handlers.</summary>
   public static void DeclareDomainEndpoint(ExactlyOnceEndpointBuilder endpointBuilder)
   {
      RegisterTypeMappings(endpointBuilder);
      RegisterDomainComponents(endpointBuilder);
      RegisterHandlers(endpointBuilder);
   }

   ///<summary>The statistics endpoint: query models maintained from the domain endpoint's tevents, stored as documents.</summary>
   public static void DeclareStatisticsEndpoint(ExactlyOnceEndpointBuilder endpointBuilder)
   {
      RegisterTypeMappings(endpointBuilder);

      endpointBuilder.RegisterDocumentDb()
              .HandleDocumentType<AccountStatistics.SingletonStatisticsQueryModel>();

      AccountStatistics.Register(endpointBuilder);
   }

   static void RegisterTypeMappings(ExactlyOnceEndpointBuilder endpointBuilder) =>
      endpointBuilder.Registrar.RequireAccountManagementTypeMappings();

   static void RegisterDomainComponents(ExactlyOnceEndpointBuilder endpointBuilder)
   {
      endpointBuilder.RegisterTeventStore()
              .HandleTaggregate<Account, IAccountTevent>();

      endpointBuilder.RegisterDocumentDb()
              .HandleDocumentType<TeventStoreApi.TueryApi.TaggregateLink<Account>>();
   }

   static void RegisterHandlers(ExactlyOnceEndpointBuilder endpointBuilder) =>
      endpointBuilder
        .RegisterTypermediaHandlers(handle =>
         {
            UIAdapterLayer.Register(handle);

            AccountQueryModel.Api.RegisterHandlers(handle);

            EmailToAccountMapper.TryGetAccountByEmail(handle);
         })
        .RegisterTessageBusHandlers(handle => EmailToAccountMapper.UpdateMappingWhenEmailChanges(handle));
}
