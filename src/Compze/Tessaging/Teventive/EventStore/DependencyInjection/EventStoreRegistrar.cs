using System;
using System.Collections.Generic;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Abstractions.Internal.Time;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Persistence.EventStore;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Tessaging.Teventive.EventStore.PersistenceLayer.Abstractions;
using Compze.Tessaging.Teventive.EventStore.Refactoring.Migrations;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
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

      registrar.Register(AggregateTypeValidator.RegisterWith,
                         EventStoreSerializer.RegisterWith,
                         EventCache.RegisterWith);

      registrar.Register(
         Scoped.For<IEventStoreUpdater, IEventStoreReader>()
               .CreatedBy((IEventStoreEventPublisher eventPublisher, IEventStore eventStore, IUtcTimeTimeSource timeSource, IAggregateTypeValidator aggregateTypeValidator) =>
                             new EventStoreUpdater(eventPublisher, eventStore, timeSource, aggregateTypeValidator)));

      return registrar;
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
