using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Tevents;
using AccountManagement.UI;
using AccountManagement.UI.QueryModels;
using Compze.Tessaging.Endpoints;
using Compze.DocumentDb.Wiring;
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
         declare: endpointBuilder =>
         {
            RegisterTypeMappings(endpointBuilder);
            RegisterDomainComponents(endpointBuilder);
            RegisterHandlers(endpointBuilder);
         });

      RegisterAccountStatisticsEndpoint(host);

      return domainEndpoint;
   }

   static void RegisterAccountStatisticsEndpoint(TestingEndpointHost host) =>
      host.RegisterExactlyOnceEndpoint(
         name: "AccountManagementStatistics",
         id: new EndpointId(Guid.Parse(input: "B16250DE-4321-4FBD-A0CC-E42C7A1B0B34")),
         declare: endpointBuilder =>
         {
            RegisterTypeMappings(endpointBuilder);

            endpointBuilder.RegisterDocumentDb()
                    .HandleDocumentType<AccountStatistics.SingletonStatisticsQueryModel>();

            AccountStatistics.Register(endpointBuilder);
         });

   static void RegisterTypeMappings(ExactlyOnceEndpointBuilder endpointBuilder) =>
      endpointBuilder.MapTypes(mapper => mapper.RegisterAccountManagementTypeMappings());

   static void RegisterDomainComponents(ExactlyOnceEndpointBuilder endpointBuilder)
   {
      endpointBuilder.RegisterTeventStore()
              .HandleTaggregate<Account, IAccountTevent>();

      endpointBuilder.RegisterDocumentDb()
              .HandleDocumentType<TeventStoreApi.TueryApi.TaggregateLink<Account>>();
   }

   static void RegisterHandlers(ExactlyOnceEndpointBuilder endpointBuilder) =>
      endpointBuilder.RegisterTessageHandlers(handle =>
      {
         UIAdapterLayer.Register(handle);

         AccountQueryModel.Api.RegisterHandlers(handle);

         EmailToAccountMapper.UpdateMappingWhenEmailChanges(handle);
         EmailToAccountMapper.TryGetAccountByEmail(handle);
      });
}
