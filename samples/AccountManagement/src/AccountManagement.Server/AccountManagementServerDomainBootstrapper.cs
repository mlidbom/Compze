using AccountManagement.Domain;
using AccountManagement.Domain.Tevents;
using AccountManagement.UI;
using AccountManagement.UI.QueryModels;
using Compze.DocumentDb.Wiring;
using Compze.Core.DocumentDb.Wiring;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Teventive.TeventStore.Wiring;
using Compze.Tessaging.TyperMediaApi.EventStore;

namespace AccountManagement;

public static class AccountManagementServerDomainBootstrapper
{
   public static IEndpoint RegisterWith(IEndpointHost host)
   {
      return host.RegisterEndpoint(name: "AccountManagement",
                                   id: new EndpointId(Guid.Parse(input: "1A1BE9C8-C8F6-4E38-ABFB-F101E5EDB00D")),
                                   setup: builder =>
                                   {
                                      RegisterDomainComponents(builder);
                                      RegisterHandlers(builder);
                                   });
   }

   static void RegisterDomainComponents(IEndpointBuilder builder)
   {
      builder.RegisterTeventStore()
             .HandleTaggregate<Account, IAccountTevent>();

      builder.RegisterDocumentDb()
             .HandleDocumentType<TeventStoreApi.TueryApi.TaggregateLink<Account>>(builder.RegisterHandlers)
             .HandleDocumentType<AccountStatistics.SingletonStatisticsQueryModel>(builder.RegisterHandlers);
   }

   static void RegisterHandlers(IEndpointBuilder builder)
   {
      UIAdapterLayer.Register(builder.RegisterHandlers);

      //todo: This should not be called synchronously. We should have it in a separate consistency boundary so that it does not slow down every operation on an account.
      AccountStatistics.Register(builder);

      AccountQueryModel.Api.RegisterHandlers(builder.RegisterHandlers);

      EmailToAccountMapper.UpdateMappingWhenEmailChanges(builder.RegisterHandlers);
      EmailToAccountMapper.TryGetAccountByEmail(builder.RegisterHandlers);
   }
}
