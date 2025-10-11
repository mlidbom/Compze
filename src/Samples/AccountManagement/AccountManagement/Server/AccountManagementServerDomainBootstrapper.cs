using System;
using AccountManagement.Domain;
using AccountManagement.Domain.Events;
using AccountManagement.UI;
using AccountManagement.UI.QueryModels;
using Compze.Sql.DocumentDb.DependencyInjection;
using Compze.Sql.DocumentDb.MicrosoftSql;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.AspNetCore.DependencyInjection;
using Compze.Tessaging.Hosting.Sql.MicrosoftSql;
using Compze.Tessaging.Sql.EventStore;
using Compze.Tessaging.Sql.MicrosoftSql;
using Compze.Tessaging.Teventive.EventStore.DependencyInjection;
using Compze.Tessaging.Teventive.EventStore.MicrosoftSql;

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
              .MsSqlEventStore()
              .MsSqlTessaging();

      builder.RegisterEventStore()
             .HandleAggregate<Account, AccountEvent.Root>();

      builder.RegisterDocumentDb()
             .HandleDocumentType<EventStoreApi.QueryApi.AggregateLink<Account>>(builder.RegisterHandlers)
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
