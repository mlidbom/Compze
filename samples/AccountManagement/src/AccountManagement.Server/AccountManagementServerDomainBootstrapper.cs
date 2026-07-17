using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Tevents;
using AccountManagement.UI;
using AccountManagement.UI.QueryModels;
using Compze.Abstractions.Hosting.Public;
using Compze.DocumentDb.Wiring;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Hosting.Testing;
using Compze.Teventive.TeventStore.Typermedia;

namespace AccountManagement;

public static class AccountManagementServerDomainBootstrapper
{
   public static ExactlyOnceEndpoint RegisterWith(TestingEndpointHost host)
   {
      var domainEndpoint = host.RegisterExactlyOnceEndpoint(
         name: "AccountManagement",
         id: new EndpointId(Guid.Parse(input: "1A1BE9C8-C8F6-4E38-ABFB-F101E5EDB00D")),
         declare: endpoint =>
         {
            RegisterTypeMappings(endpoint);
            RegisterDomainComponents(endpoint);
            RegisterHandlers(endpoint);
         });

      RegisterAccountStatisticsEndpoint(host);

      return domainEndpoint;
   }

   static void RegisterAccountStatisticsEndpoint(TestingEndpointHost host) =>
      host.RegisterExactlyOnceEndpoint(
         name: "AccountManagement.Statistics",
         id: new EndpointId(Guid.Parse(input: "B16250DE-4321-4FBD-A0CC-E42C7A1B0B34")),
         declare: endpoint =>
         {
            RegisterTypeMappings(endpoint);

            endpoint.RegisterDocumentDb()
                    .HandleDocumentType<AccountStatistics.SingletonStatisticsQueryModel>();

            AccountStatistics.Register(endpoint);
         });

   static void RegisterTypeMappings(ExactlyOnceEndpointBuilder endpoint) =>
      endpoint.MapTypes(mapper => mapper.RegisterAccountManagementTypeMappings());

   static void RegisterDomainComponents(ExactlyOnceEndpointBuilder endpoint)
   {
      endpoint.RegisterTeventStore()
              .HandleTaggregate<Account, IAccountTevent>();

      endpoint.RegisterDocumentDb()
              .HandleDocumentType<TeventStoreApi.TueryApi.TaggregateLink<Account>>();
   }

   static void RegisterHandlers(ExactlyOnceEndpointBuilder endpoint) =>
      endpoint.RegisterTessageHandlers(handle =>
      {
         UIAdapterLayer.Register(handle);

         AccountQueryModel.Api.RegisterHandlers(handle);

         EmailToAccountMapper.UpdateMappingWhenEmailChanges(handle);
         EmailToAccountMapper.TryGetAccountByEmail(handle);
      });
}
