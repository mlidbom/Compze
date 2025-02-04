﻿using System;
using Compze.Messaging.Events;

namespace Compze.Persistence.EventStore.Aggregates;

public partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
   where TWrapperEventImplementation : TWrapperEventInterface
   where TWrapperEventInterface : IAggregateWrapperEvent<TAggregateEvent>
   where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
   where TAggregateEvent : class, IAggregateEvent
   where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
{
   public abstract class RemovableEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
      : Entity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
      where TEntityId : struct
      where TEntityEvent : class, TAggregateEvent
      where TEntityEventImplementation : TAggregateEventImplementation, TEntityEvent
      where TEntityCreatedEvent : TEntityEvent
      where TEntityRemovedEvent : TEntityEvent
      where TEntity : RemovableEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
      where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
   {
      static RemovableEntity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

      protected RemovableEntity(TAggregate aggregate) : base(aggregate)
      {
         RegisterEventAppliers()
           .IgnoreUnhandled<TEntityRemovedEvent>();
      }

      public new static CollectionManager CreateSelfManagingCollection(TAggregate parent)
         => new(parent: parent, raiseEventThroughParent: @event => parent.Publish(@event), appliersRegistrar: parent.RegisterEventAppliers());

      public new class CollectionManager : EntityCollectionManager<TAggregate, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
      {
         internal CollectionManager(TAggregate parent, Action<TEntityEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
            : base(parent, raiseEventThroughParent, appliersRegistrar) {}
      }
   }
}
