using System;
using System.Collections.Generic;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.EventStore;
using Compze.EventStore.Abstractions;
using Compze.EventStore.Refactoring.Migrations;
using Compze.GenericAbstractions.Time;
using Compze.Persistence.EventStore;
using Compze.Teventive.Aggregates;
using Compze.Refactoring.Naming;
using Compze.Serialization;
using Compze.Tessaging.Buses;
using Compze.EventStore.PersistenceLayer.Abstractions;

namespace Compze.Persistence.Common.DependencyInjection;

public static class EventStoreRegistrar
{
   static readonly IEventMigration[] EmptyMigrationsArray = [];

   public static EventStoreRegistrationBuilder RegisterEventStore(this IEndpointBuilder @this) => @this.RegisterEventStore(EmptyMigrationsArray);
   public static EventStoreRegistrationBuilder RegisterEventStore(this IEndpointBuilder @this, IReadOnlyList<IEventMigration> migrations)
   {
      @this.Container.RegisterEventStore(@this.Configuration.ConnectionStringName, migrations);
      return new EventStoreRegistrationBuilder(@this.RegisterHandlers);
   }

   public static void RegisterEventStore(this IDependencyInjectionContainer @this, string connectionName) =>
      @this.RegisterEventStore(connectionName, EmptyMigrationsArray);

   public static void RegisterEventStore(this IDependencyInjectionContainer @this,
                                         string connectionName,
                                         IReadOnlyList<IEventMigration> migrations) =>
      @this.RegisterEventStoreForFlexibleTesting(connectionName, () => migrations);

   public static void RegisterEventStoreForFlexibleTesting(this IDependencyInjectionContainer @this,
                                                           string connectionName,
                                                           Func<IReadOnlyList<IEventMigration>> migrations)
   {
      Assert.Argument.NotNullEmptyOrWhitespace(connectionName);

      @this.Register(
         Singleton.For<IAggregateTypeValidator>()
                  .CreatedBy((ITypeMapper typeMapper) => new AggregateTypeValidator(typeMapper)),
         Singleton.For<IEventStoreSerializer>()
                  .CreatedBy((ITypeMapper typeMapper) => new EventStoreSerializer(typeMapper)),
         Singleton.For<EventCache, IEventCache>()
                  .CreatedBy(() => new EventCache()),
         Scoped.For<IEventStore>()
               .CreatedBy((IEventStorePersistenceLayer persistenceLayer, ITypeMapper typeMapper, IEventStoreSerializer serializer, EventCache cache) =>
                             new Compze.EventStore.EventStore(persistenceLayer, typeMapper, serializer, cache, migrations())),
         Scoped.For<IEventStoreUpdater, IEventStoreReader>()
               .CreatedBy((IEventStoreEventPublisher eventPublisher, IEventStore eventStore, IUtcTimeTimeSource timeSource, IAggregateTypeValidator aggregateTypeValidator) =>
                             new EventStoreUpdater(eventPublisher, eventStore, timeSource, aggregateTypeValidator)));
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