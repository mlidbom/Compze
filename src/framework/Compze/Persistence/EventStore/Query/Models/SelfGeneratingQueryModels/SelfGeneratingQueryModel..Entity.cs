using Compze.Contracts;
using Compze.Messaging.Events;
using Compze.Persistence.EventStore.Aggregates;
using Compze.SystemCE.ReflectionCE;
using JetBrains.Annotations;

namespace Compze.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
   where TAggregateEvent : class, IAggregateEvent
{
   [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
   public abstract class Entity<TEntity, TEntityId, TEntityEvent, TEntityCreatedEvent, TEventEntityIdGetter> : Component<TEntity, TEntityEvent>
      where TEntityId : notnull
      where TEntityEvent : class, TAggregateEvent
      where TEntityCreatedEvent : TEntityEvent
      where TEntity : Entity<TEntity, TEntityId, TEntityEvent, TEntityCreatedEvent, TEventEntityIdGetter>
      where TEventEntityIdGetter : IGetAggregateEntityEventEntityId<TEntityEvent, TEntityId>
   {
      static readonly TEventEntityIdGetter IdGetter = Constructor.For<TEventEntityIdGetter>.DefaultConstructor.Instance();

      TEntityId _id;
      public TEntityId Id => Assert.Result.ReturnNotNullOrDefault(_id);

      protected Entity(TQueryModel queryModel) : this(queryModel.RegisterEventAppliers()) {}

#pragma warning disable CS8618 //Review OK-ish: We guarantee that we never deliver out a null or default value from the public property.
      Entity(IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(appliersRegistrar, registerEventAppliers: false)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
      {
         RegisterEventAppliers()
           .For<TEntityCreatedEvent>(e => _id = IdGetter.GetId(e));
      }

      public static CollectionManager CreateSelfManagingCollection(TQueryModel parent) => new(parent, parent.RegisterEventAppliers());

      public class CollectionManager : QueryModelEntityCollectionManager<TQueryModel,TEntity, TEntityId, TEntityEvent, TEntityCreatedEvent, TEventEntityIdGetter>
      {
         internal CollectionManager(TQueryModel parent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(parent, appliersRegistrar) {}
      }
   }
}
