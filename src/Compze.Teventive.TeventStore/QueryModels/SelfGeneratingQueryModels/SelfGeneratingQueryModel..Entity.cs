using Compze.Tessaging.Teventive.Public;
using Compze.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Contracts;
using Compze.Internals.SystemCE.ReflectionCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TTaggregateTevent : class, ITaggregateTevent
{
   [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
   public abstract class Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter> : Component<TEntity, TEntityTevent>
      where TEntityId : struct
      where TEntityTevent : class, TTaggregateTevent
      where TEntityCreatedTevent : TEntityTevent
      where TEntity : Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>
      where TTeventEntityIdGetter : IGetTaggregateEntityTeventEntityId<TEntityTevent, TEntityId>
   {
      static readonly TTeventEntityIdGetter IdGetter = Constructor.For<TTeventEntityIdGetter>.DefaultConstructor.Instance();

      TEntityId _id;
      public TEntityId Id => _id._assert().NotDefault();

      protected Entity(TQueryModel queryModel) : this(queryModel.RegisterTeventAppliers()) {}

#pragma warning disable CS8618 //Reviewed OK-ish: We guarantee that we never deliver out a null or default value from the public property.
      Entity(ITeventHandlerRegistrar<TEntityTevent> appliersRegistrar) : base(appliersRegistrar, registerTeventAppliers: false)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
      {
         RegisterTeventAppliers()
           .For<TEntityCreatedTevent>(e => _id = IdGetter.GetId(e));
      }

      public static CollectionManager CreateSelfManagingCollection(TQueryModel parent) => new(parent, parent.RegisterTeventAppliers());

      public class CollectionManager : QueryModelEntityCollectionManager<TQueryModel,TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>
      {
         internal CollectionManager(TQueryModel parent, ITeventHandlerRegistrar<TEntityTevent> appliersRegistrar) : base(parent, appliersRegistrar) {}
      }
   }
}
