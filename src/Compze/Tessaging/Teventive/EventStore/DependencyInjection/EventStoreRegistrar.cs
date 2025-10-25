using System;
using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Hosting.MessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Refactoring.Migrations.Public;
using Compze.Serialization;
using Compze.Tessaging.Teventive.EventStore.Refactoring.Migrations;
using Compze.Tessaging.TyperMediaApi.EventStore;
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

   public static IComponentRegistrar EventStore(this IComponentRegistrar registrar, string connectionName) =>
      registrar.EventStore(connectionName, EmptyMigrationsArray);

   public static IComponentRegistrar EventStore(this IComponentRegistrar @this,
                                                 string connectionName,
                                                 IReadOnlyList<IEventMigration> migrations) =>
      @this.EventStoreForFlexibleTesting(connectionName, () => migrations);

   public static IComponentRegistrar EventStoreForFlexibleTesting(this IComponentRegistrar registrar,
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
