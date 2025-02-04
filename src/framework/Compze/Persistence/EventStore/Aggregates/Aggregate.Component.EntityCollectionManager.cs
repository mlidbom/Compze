﻿using System;
using Compze.Messaging.Events;
using Compze.SystemCE.ReflectionCE;

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
      public class EntityCollectionManager<TParent, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
         : IEntityCollectionManager<TEntity, TEntityId, TEntityEvent, TEntityEventImplementation, TEntityCreatedEvent>
         where TEntityId : notnull
         where TEntityEvent : class, TAggregateEvent
         where TEntityCreatedEvent : TEntityEvent
         where TEntityEventImplementation : TEntityEvent, TAggregateEventImplementation
         where TEntity : Component<TEntity, TEntityEventImplementation, TEntityEvent>
         where TEntityEventIdGetterSetter : IGetAggregateEntityEventEntityId<TEntityEvent, TEntityId>
      {
         protected static readonly TEntityEventIdGetterSetter IdGetter = Constructor.For<TEntityEventIdGetterSetter>.DefaultConstructor.Instance();

         protected EntityCollection<TEntity, TEntityId> ManagedEntities { get; }
         readonly Action<TEntityEventImplementation> _raiseEventThroughParent;

         protected EntityCollectionManager(TParent parent,
                                           Action<TEntityEventImplementation> raiseEventThroughParent,
                                           IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
         {
            ManagedEntities = [];
            _raiseEventThroughParent = raiseEventThroughParent;
            appliersRegistrar
              .For<TEntityCreatedEvent>(
                  e =>
                  {
                     var entity = Constructor.For<TEntity>.WithArguments<TParent>.Instance(parent);
                     ManagedEntities.Add(entity, IdGetter.GetId(e));
                  })
              .For<TEntityEvent>(e => ManagedEntities[IdGetter.GetId(e)].ApplyEvent(e));
         }

         public IReadOnlyEntityCollection<TEntity, TEntityId> Entities => ManagedEntities;

         public TEntity AddByPublishing<TCreationEvent>(TCreationEvent creationEvent) where TCreationEvent : TEntityEventImplementation, TEntityCreatedEvent
         {
            _raiseEventThroughParent(creationEvent);
            var result = ManagedEntities.InCreationOrder[^1];
            result._eventHandlersEventDispatcher.Dispatch(creationEvent);
            return result;
         }
      }
   }
}
