using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TTaggregateTevent : class, ITaggregateTevent
{
   public abstract class Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter> :
      Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>
      where TEntityId : struct
      where TEntityTevent : class, TTaggregateTevent
      where TEntityCreatedTevent : TEntityTevent
      where TEntityRemovedTevent : TEntityTevent
      where TEntity : Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>
      where TTeventEntityIdGetter : IGetTaggregateEntityTeventEntityId<TEntityTevent, TEntityId>
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
