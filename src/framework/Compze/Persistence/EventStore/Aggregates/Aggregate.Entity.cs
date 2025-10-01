using System;
using Compze.Messaging.Events;
using JetBrains.Annotations;

namespace Compze.Persistence.EventStore.Aggregates;

public partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
   where TWrapperEventImplementation : TWrapperEventInterface
   where TWrapperEventInterface : IAggregateWrapperEvent<TAggregateEvent>
   where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
   where TAggregateEvent : class, IAggregateEvent
   where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
{
   [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
   public abstract class AggregateEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
      : EventiveEntity<TAggregate, TAggregateEvent, TAggregateEventImplementation, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
      where TEntityId : struct
      where TEntityEvent : class, TAggregateEvent
      where TEntityEventImplementation : TAggregateEventImplementation, TEntityEvent
      where TEntityCreatedEvent : TEntityEvent
      where TEntity : AggregateEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
      where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
   {
       protected AggregateEntity(TAggregate aggregate) : base(aggregate) {}
       protected AggregateEntity(Action<TEntityEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(raiseEventThroughParent, appliersRegistrar) {}
   }
}
