using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Tuery.Models.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TAggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TAggregateTevent>
   where TAggregateTevent : class, IAggregateTevent
{
   public abstract class Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter> :
      Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>
      where TEntityId : struct
      where TEntityTevent : class, TAggregateTevent
      where TEntityCreatedTevent : TEntityTevent
      where TEntityRemovedTevent : TEntityTevent
      where TEntity : Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>
      where TTeventEntityIdGetter : IGetAggregateEntityTeventEntityId<TEntityTevent, TEntityId>
   {
      protected Entity(TQueryModel queryModel) : base(queryModel)
      {
         RegisterTeventAppliers()
           .IgnoreUnhandled<TEntityRemovedTevent>();
      }

      public new static CollectionManager CreateSelfManagingCollection(TQueryModel parent) => new(parent: parent, appliersRegistrar: parent.RegisterTeventAppliers());

      public new class CollectionManager : QueryModelEntityCollectionManager<TQueryModel, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>
      {
         internal CollectionManager(TQueryModel parent, ITeventHandlerRegistrar<TEntityTevent> appliersRegistrar) : base(parent, appliersRegistrar) {}
      }
   }
}
