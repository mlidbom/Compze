using Compze.Messaging.Events;
using Compze.Persistence.EventStore.Aggregates;

namespace Compze.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
   where TAggregateEvent : class, IAggregateEvent
{
   public abstract partial class Component<TComponent, TComponentEvent>
      where TComponentEvent : class, TAggregateEvent
      where TComponent : Component<TComponent, TComponentEvent>
   {
      public abstract class RemovableNestedEntity<TEntity, TEntityId, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEventEntityIdGetter>
         : NestedEntity<TEntity, TEntityId, TEntityEvent, TEntityCreatedEvent, TEventEntityIdGetter>
         where TEntityId : struct
         where TEntityEvent : class, TComponentEvent
         where TEntityCreatedEvent : TEntityEvent
         where TEntityRemovedEvent : TEntityEvent
         where TEventEntityIdGetter : IGetAggregateEntityEventEntityId<TEntityEvent, TEntityId>
         where TEntity : NestedEntity<TEntity, TEntityId, TEntityEvent, TEntityCreatedEvent, TEventEntityIdGetter>
      {
         protected RemovableNestedEntity(TComponent parent) : this(parent.RegisterEventAppliers()) {}

         RemovableNestedEntity(IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(appliersRegistrar)
         {
            RegisterEventAppliers()
              .IgnoreUnhandled<TEntityRemovedEvent>();
         }

         public new static CollectionManager CreateSelfManagingCollection(TComponent parent) => new(parent: parent, appliersRegistrar: parent.RegisterEventAppliers());

         public new class CollectionManager : QueryModelEntityCollectionManager<TComponent, TEntity, TEntityId, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEventEntityIdGetter>
         {
            internal CollectionManager(TComponent parent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(parent, appliersRegistrar) {}
         }
      }
   }
}
