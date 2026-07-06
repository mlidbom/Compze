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
      public abstract class RemovableNestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>
         : NestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>
         where TEntityId : struct
         where TEntityTevent : class, TComponentTevent
         where TEntityCreatedTevent : TEntityTevent
         where TEntityRemovedTevent : TEntityTevent
         where TTeventEntityIdGetter : IGetTaggregateEntityTeventEntityId<TEntityTevent, TEntityId>
         where TEntity : NestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>
      {
         protected RemovableNestedEntity(TComponent parent) : this(parent.RegisterTeventAppliers()) {}

         RemovableNestedEntity(ITeventHandlerRegistrar<TEntityTevent> appliersRegistrar)
            : base(appliersRegistrar, TeventDispatcherConfig.Default.IgnoreUnhandled<TEntityRemovedTevent>()) {}

         public new static ICollectionManager CreateSelfManagingCollection(TComponent parent) => new CollectionManager(parent: parent, appliersRegistrar: parent.RegisterTeventAppliers());

         public interface ICollectionManager : IQueryModelEntityCollectionManager<TEntity, TEntityId>;

         new class CollectionManager : QueryModelEntityCollectionManager<TComponent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>, ICollectionManager
         {
            internal CollectionManager(TComponent parent, ITeventHandlerRegistrar<TEntityTevent> appliersRegistrar) : base(parent, appliersRegistrar) {}
         }
      }
   }
}
