using System;
using Compze.Contracts;
using Compze.GenericAbstractions.Time;
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
      public abstract class NestedEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
         : Entity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
         where TEntityId : struct
         where TEntityEvent : class, TComponentEvent
         where TEntityEventImplementation : TComponentEventImplementation, TEntityEvent
         where TEntityCreatedEvent : TEntityEvent
         where TEntity : NestedEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
         where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
      {
         static NestedEntity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

         protected NestedEntity(Action<TEntityEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(raiseEventThroughParent, appliersRegistrar) {}
      }
   }
}
