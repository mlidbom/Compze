using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TTaggregateTevent : class, ITaggregateTevent
{
   public abstract partial class Component<TComponent, TComponentTevent>
      where TComponentTevent : class, TTaggregateTevent
      where TComponent : Component<TComponent, TComponentTevent>
   {
      public class QueryModelEntityCollectionManager<TParent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>
         : QueryModelEntityCollectionManager<TParent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>
         where TEntityId : notnull
         where TEntityTevent : class, TTaggregateTevent
         where TEntityCreatedTevent : TEntityTevent
         where TEntityRemovedTevent : TEntityTevent
         where TEntity : Component<TEntity, TEntityTevent>
         where TTeventEntityIdGetter : IGetTaggregateEntityTeventEntityId<TEntityTevent, TEntityId>
      {
         protected QueryModelEntityCollectionManager(TParent parent, ITeventSubscriber<TEntityTevent> appliersRegistrar) : base(parent, appliersRegistrar)
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
