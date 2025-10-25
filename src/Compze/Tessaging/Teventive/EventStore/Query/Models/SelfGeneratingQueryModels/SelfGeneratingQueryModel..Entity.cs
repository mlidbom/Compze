using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Utilities.Contracts;
using Compze.Utilities.SystemCE.ReflectionCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Teventive.TeventStore.Tuery.Models.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TAggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TAggregateTevent>
   where TAggregateTevent : class, IAggregateTevent
{
   [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
   public abstract class Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter> : Component<TEntity, TEntityTevent>
      where TEntityId : struct
      where TEntityTevent : class, TAggregateTevent
      where TEntityCreatedTevent : TEntityTevent
      where TEntity : Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>
      where TTeventEntityIdGetter : IGetAggregateEntityTeventEntityId<TEntityTevent, TEntityId>
   {
      static readonly TTeventEntityIdGetter IdGetter = Constructor.For<TTeventEntityIdGetter>.DefaultConstructor.Instance();

      TEntityId _id;
      public TEntityId Id => Assert.Result.ReturnNotDefault(_id);

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
