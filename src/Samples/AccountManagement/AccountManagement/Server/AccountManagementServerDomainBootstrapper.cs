using System;
using AccountManagement.Domain;
using AccountManagement.Domain.Tevents;
using AccountManagement.UI;
using AccountManagement.UI.QueryModels;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Transport.Internal;
using Compze.Sql.DocumentDb.MicrosoftSql.Wiring;
using Compze.Sql.DocumentDb.Wiring;
using Compze.Sql.MicrosoftSql;
using Compze.Tessaging.Hosting.AspNetCore.Wiring;
using Compze.Tessaging.Sql.MicrosoftSql;
using Compze.Tessaging.Teventive.TeventStore.DependencyInjection;
using Compze.Tessaging.Teventive.TeventStore.MicrosoftSql;
using Compze.Tessaging.TyperMediaApi.TeventStore;

namespace AccountManagement;

public class AccountManagementServerDomainBootstrapper
{
   public IEndpoint RegisterWith(IEndpointHost host)
   {
      return host.RegisterEndpoint(name: "AccountManagement",
                                   id: new EndpointId(Guid.Parse(input: "1A1BE9C8-C8F6-4E38-ABFB-F101E5EDB00D")),
                                   setup: builder =>
                                   {
                                      builder.Container.Register()
                                             .AspNetCoreTransport();
                                      RegisterDomainComponents(builder);
                                      RegisterHandlers(builder);
                                   });
   }

   static void RegisterDomainComponents(IEndpointBuilder builder)
   {
      var connectionStringName = builder.Configuration.ConnectionStringName;
      var register = builder.Container.Register();
      register.MsSqlConnectionPool(connectionStringName)
              .MsSqlDocumentDb()
              .MsSqlTeventStore()
              .MsSqlTessaging();

      builder.RegisterTeventStore()
             .HandleAggregate<Account, AccountTevent.Root>();

      builder.RegisterDocumentDb()
             .HandleDocumentType<TeventStoreApi.TueryApi.AggregateLink<Account>>(builder.RegisterHandlers)
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
