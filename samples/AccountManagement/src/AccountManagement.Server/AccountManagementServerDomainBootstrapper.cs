using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Tevents;
using AccountManagement.UI;
using AccountManagement.UI.QueryModels;
using Compze.Core.DocumentDb.Wiring;
using Compze.Abstractions.Hosting.Public;
using Compze.Hosting;
using Compze.Tessaging.Teventive.TeventStore.Typermedia;

namespace AccountManagement;

public static class AccountManagementServerDomainBootstrapper
{
   public static IEndpoint RegisterWith(IEndpointHost host)
   {
      var domainEndpoint = host.RegisterEndpoint(name: "AccountManagement",
                                   id: new EndpointId(Guid.Parse(input: "1A1BE9C8-C8F6-4E38-ABFB-F101E5EDB00D")),
                                   setup: builder =>
                                   {
                                      RegisterTypeMappings(builder);
                                      RegisterDomainComponents(builder);
                                      RegisterHandlers(builder);
                                   });

      RegisterAccountStatisticsEndpoint(host);

      return domainEndpoint;
   }

   static void RegisterAccountStatisticsEndpoint(IEndpointHost host) =>
      host.RegisterEndpoint(name: "AccountManagement.Statistics",
                            id: new EndpointId(Guid.Parse(input: "B16250DE-4321-4FBD-A0CC-E42C7A1B0B34")),
                            setup: builder =>
                            {
                               RegisterTypeMappings(builder);

                               builder.RegisterDocumentDb()
                                      .HandleDocumentType<AccountStatistics.SingletonStatisticsQueryModel>(builder.RegisterTypermediaHandlers);

                               AccountStatistics.Register(builder);
                            });

   static void RegisterTypeMappings(IEndpointBuilder builder)
   {
      builder.TypeMapper.RegisterAccountManagementTypeMappings();
   }

   static void RegisterDomainComponents(IEndpointBuilder builder)
   {
      builder.RegisterTeventStore()
             .HandleTaggregate<Account, IAccountTevent>();

      builder.RegisterDocumentDb()
             .HandleDocumentType<TeventStoreApi.TueryApi.TaggregateLink<Account>>(builder.RegisterTypermediaHandlers);
   }

   static void RegisterHandlers(IEndpointBuilder builder)
   {
      UIAdapterLayer.Register(builder.RegisterTypermediaHandlers);

      AccountQueryModel.Api.RegisterHandlers(builder.RegisterTypermediaHandlers);

      EmailToAccountMapper.UpdateMappingWhenEmailChanges(builder.RegisterTessagingHandlers);
      EmailToAccountMapper.TryGetAccountByEmail(builder.RegisterTypermediaHandlers);
   }
}
