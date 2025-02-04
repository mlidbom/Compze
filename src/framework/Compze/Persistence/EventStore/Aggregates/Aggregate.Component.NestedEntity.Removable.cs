﻿using System;
using Compze.GenericAbstractions.Time;
using Compze.Messaging.Events;

namespace Compze.Persistence.EventStore.Aggregates;

public partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
   where TWrapperEventImplementation : TWrapperEventInterface
   where TWrapperEventInterface : IAggregateWrapperEvent<TAggregateEvent>
   where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
   where TAggregateEvent : class, IAggregateEvent
   where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
{
   public abstract partial class Component<TComponent, TComponentEventImplementation, TComponentEvent>
      where TComponentEvent : class, TAggregateEvent
      where TComponentEventImplementation : TAggregateEventImplementation, TComponentEvent
      where TComponent : Component<TComponent, TComponentEventImplementation, TComponentEvent>
   {
      public abstract class RemovableNestedEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
         : NestedEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
         where TEntityId : struct
         where TEntityEvent : class, TComponentEvent
         where TEntityEventImplementation : TComponentEventImplementation, TEntityEvent
         where TEntityCreatedEvent : TEntityEvent
         where TEntityRemovedEvent : TEntityEvent
         where TEntityEventIdGetterSetter :
         IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
         where TEntity : NestedEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
      {
         static RemovableNestedEntity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

         protected RemovableNestedEntity(TComponent parent) : this(parent.TimeSource, parent.Publish, parent.RegisterEventAppliers()) {}

         RemovableNestedEntity(IUtcTimeTimeSource timeSource, Action<TEntityEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
            : base(timeSource, raiseEventThroughParent, appliersRegistrar)
         {
            RegisterEventAppliers()
              .IgnoreUnhandled<TEntityRemovedEvent>();
         }

         public new static CollectionManager CreateSelfManagingCollection(TComponent parent)
            => new(parent: parent, raiseEventThroughParent: parent.Publish, appliersRegistrar: parent.RegisterEventAppliers());

         public new class CollectionManager : EntityCollectionManager<TComponent, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
         {
            internal CollectionManager(TComponent parent, Action<TEntityEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
               : base(parent, raiseEventThroughParent, appliersRegistrar) {}
         }
      }
   }
}
