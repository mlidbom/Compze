using Compze.Messaging.Events;
using Compze.Persistence.EventStore.Aggregates;

namespace Compze.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
   where TAggregateEvent : class, IAggregateEvent
{
   public abstract class Entity<TEntity,
                                TEntityId,
                                TEntityEvent,
                                TEntityCreatedEvent,
                                TEntityRemovedEvent,
                                TEventEntityIdGetter> : Entity<TEntity,
      TEntityId,
      TEntityEvent,
      TEntityCreatedEvent,
      TEventEntityIdGetter>
      where TEntityId : notnull
      where TEntityEvent : class, TAggregateEvent
      where TEntityCreatedEvent : TEntityEvent
      where TEntityRemovedEvent : TEntityEvent
      where TEntity : Entity<TEntity,
         TEntityId,
         TEntityEvent,
         TEntityCreatedEvent,
         TEntityRemovedEvent,
         TEventEntityIdGetter>
      where TEventEntityIdGetter : IGetAggregateEntityEventEntityId<TEntityEvent, TEntityId>
   {
      protected Entity(TQueryModel queryModel) : base(queryModel)
      {
         RegisterEventAppliers()
           .IgnoreUnhandled<TEntityRemovedEvent>();
      }
      public new static CollectionManager CreateSelfManagingCollection(TQueryModel parent) => new(parent: parent, appliersRegistrar: parent.RegisterEventAppliers());

      public new class CollectionManager : QueryModelEntityCollectionManager<TQueryModel,
         TEntity,
         TEntityId,
         TEntityEvent,
         TEntityCreatedEvent,
         TEntityRemovedEvent,
         TEventEntityIdGetter>
      {
         internal CollectionManager(TQueryModel parent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar): base(parent, appliersRegistrar) {}
      }
   }
}