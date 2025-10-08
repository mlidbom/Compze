using System;
using System.Collections.Generic;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Persistence.EventStore;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Tessaging.Teventive.EventStore.Refactoring.Migrations;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Teventive.EventStore.DependencyInjection;

public static class EventStoreRegistrar
{
   static readonly IEventMigration[] EmptyMigrationsArray = [];

   public static EventStoreRegistrationBuilder RegisterEventStore(this IEndpointBuilder @this) => @this.RegisterEventStore(EmptyMigrationsArray);

   public static EventStoreRegistrationBuilder RegisterEventStore(this IEndpointBuilder @this, IReadOnlyList<IEventMigration> migrations)
   {
      @this.Container.Register().EventStore(@this.Configuration.ConnectionStringName, migrations);
      return new EventStoreRegistrationBuilder(@this.RegisterHandlers);
   }

   public static IDependencyRegistrar EventStore(this IDependencyRegistrar registrar, string connectionName) =>
      registrar.EventStore(connectionName, EmptyMigrationsArray);

   public static IDependencyRegistrar EventStore(this IDependencyRegistrar @this,
                                                 string connectionName,
                                                 IReadOnlyList<IEventMigration> migrations) =>
      @this.EventStoreForFlexibleTesting(connectionName, () => migrations);

   public static IDependencyRegistrar EventStoreForFlexibleTesting(this IDependencyRegistrar registrar,
                                                                   string connectionName,
                                                                   Func<IReadOnlyList<IEventMigration>> migrations)
   {
      Assert.Argument.NotNullEmptyOrWhitespace(connectionName);

      Teventive.EventStore.EventStore.RegisterWith(registrar, migrations);

      return registrar.Register(AggregateTypeValidator.RegisterWith,
                                EventStoreSerializer.RegisterWith,
                                EventCache.RegisterWith,
                                EventStoreUpdater.RegisterWith);
   }
}

public class EventStoreRegistrationBuilder
{
   readonly MessageHandlerRegistrarWithDependencyInjectionSupport _handlerRegistrar;
   internal EventStoreRegistrationBuilder(MessageHandlerRegistrarWithDependencyInjectionSupport handlerRegistrar) => _handlerRegistrar = handlerRegistrar;

   public EventStoreRegistrationBuilder HandleAggregate<TAggregate, TEvent>()
      where TAggregate : class, IEventStored<TEvent>
      where TEvent : IAggregateEvent
   {
      EventStoreApi.RegisterHandlersForAggregate<TAggregate, TEvent>(_handlerRegistrar);
      return this;
   }
}
