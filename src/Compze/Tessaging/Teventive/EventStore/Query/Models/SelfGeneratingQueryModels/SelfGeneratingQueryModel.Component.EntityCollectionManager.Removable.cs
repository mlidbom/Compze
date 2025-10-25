using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Tuery.Models.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TAggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TAggregateTevent>
   where TAggregateTevent : class, IAggregateTevent
{
   public abstract partial class Component<TComponent, TComponentTevent>
      where TComponentTevent : class, TAggregateTevent
      where TComponent : Component<TComponent, TComponentTevent>
   {
      public class QueryModelEntityCollectionManager<TParent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>
         : QueryModelEntityCollectionManager<TParent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>
         where TEntityId : notnull
         where TEntityTevent : class, TAggregateTevent
         where TEntityCreatedTevent : TEntityTevent
         where TEntityRemovedTevent : TEntityTevent
         where TEntity : Component<TEntity, TEntityTevent>
         where TTeventEntityIdGetter : IGetAggregateEntityTeventEntityId<TEntityTevent, TEntityId>
      {
         protected QueryModelEntityCollectionManager(TParent parent, ITeventHandlerRegistrar<TEntityTevent> appliersRegistrar) : base(parent, appliersRegistrar)
         {
            appliersRegistrar.For<TEntityRemovedTevent>(
               e =>
               {
                  var id = IdGetter.GetId(e);
                  ManagedEntities.Remove(id);
               });
         }
      }
   }
}
