using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Contracts;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TTaggregateTevent : class, ITaggregateTevent
{
   public abstract partial class Component<TComponent, TComponentTevent>
      where TComponentTevent : class, TTaggregateTevent
      where TComponent : Component<TComponent, TComponentTevent>
   {
      public abstract class NestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>
         : NestedComponent<TEntity, TEntityTevent>
         where TEntityId : struct
         where TEntityTevent : class, TComponentTevent
         where TEntityCreatedTevent : TEntityTevent
         where TEntity : NestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>
         where TTeventEntityIdGetter : IGetTaggregateEntityTeventEntityId<TEntityTevent, TEntityId>
      {
         static readonly TTeventEntityIdGetter IdGetter = Constructor.For<TTeventEntityIdGetter>.DefaultConstructor.Instance();

         protected NestedEntity(TComponent parent) : this(parent.RegisterTeventAppliers()) {}

#pragma warning disable CS8618 //Reviewed OK-ish: We guarantee that we never deliver out a null or default value from the public property.
         protected NestedEntity(ITeventHandlerRegistrar<TEntityTevent> appliersRegistrar) : base(appliersRegistrar, registerTeventAppliers: false)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
         {
            RegisterTeventAppliers()
              .For<TEntityCreatedTevent>(e => _id = IdGetter.GetId(e));
         }

         TEntityId _id;
         public TEntityId Id => _id._assert().NotDefault();

         public static CollectionManager CreateSelfManagingCollection(TComponent parent) //todo:tests
            => new(parent: parent, appliersRegistrar: parent.RegisterTeventAppliers());

         public class CollectionManager : QueryModelEntityCollectionManager<TComponent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>
         {
            internal CollectionManager(TComponent parent, ITeventHandlerRegistrar<TEntityTevent> appliersRegistrar) : base(parent, appliersRegistrar) {}
         }
      }
   }
}
