using System;
using AccountManagement.Domain;
using AccountManagement.Domain.Events;
using AccountManagement.UI;
using AccountManagement.UI.QueryModels;
using Compze.Persistence.DocumentDb.DependencyInjection;
using Compze.Persistence.DocumentDb.MicrosoftSql;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Http.DependencyInjection;
using Compze.Tessaging.Hosting.Persistence.MicrosoftSql;
using Compze.Tessaging.Persistence.EventStore;
using Compze.Tessaging.Persistence.MicrosoftSql;
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
                                      builder.Container.RegisterHttpTransport();
                                      AccountManagementApiTypeMapper.MapTypes(builder.TypeMapper);
                                      DomainTypeMapper.MapTypes(builder.TypeMapper);
                                      RegisterDomainComponents(builder);
                                      RegisterHandlers(builder);
                                   });
   }

   static void RegisterDomainComponents(IEndpointBuilder builder)
   {
      var connectionStringName = builder.Configuration.ConnectionStringName;
      builder.Container.RegisterMsSqlConnectionPoolIfNotAlreadyRegistered(connectionStringName);
      builder.Container.RegisterMsSqlDocumentDb();
      builder.Container.RegisterMsSqlEventStore();
      builder.Container.RegisterMsSqlTessaging();

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
