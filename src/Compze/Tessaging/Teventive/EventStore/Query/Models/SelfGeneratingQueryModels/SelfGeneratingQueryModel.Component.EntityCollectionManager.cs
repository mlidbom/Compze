using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Teventive.TeventStore.Tuery.Models.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TAggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TAggregateTevent>
   where TAggregateTevent : class, IAggregateTevent
{
   public abstract partial class Component<TComponent, TComponentTevent>
      where TComponentTevent : class, TAggregateTevent
      where TComponent : Component<TComponent, TComponentTevent>
   {
      public class QueryModelEntityCollectionManager<TParent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
         : IQueryModelEntityCollectionManager<TEntity, TEntityId>
         where TEntityId : notnull
         where TEntityTevent : class, TAggregateTevent
         where TEntityCreatedTevent : TEntityTevent
         where TEntity : Component<TEntity, TEntityTevent>
         where TEntityTeventIdGetterSetter : IGetAggregateEntityTeventEntityId<TEntityTevent, TEntityId>
      {
         protected static readonly TEntityTeventIdGetterSetter IdGetter = Constructor.For<TEntityTeventIdGetterSetter>.DefaultConstructor.Instance();

         protected QueryModelEntityCollection<TEntity, TEntityId> ManagedEntities { get; }

         protected QueryModelEntityCollectionManager(TParent parent, ITeventHandlerRegistrar<TEntityTevent> appliersRegistrar)
         {
            ManagedEntities = [];
            appliersRegistrar
              .For<TEntityCreatedTevent>(
                  e =>
                  {
                     var entity = Constructor.For<TEntity>.WithArguments<TParent>.Instance(parent);
                     ManagedEntities.Add(entity, IdGetter.GetId(e));
                  })
              .For<TEntityTevent>(e => ManagedEntities[IdGetter.GetId(e)].ApplyTevent(e));
         }

         public IReadonlyQueryModelEntityCollection<TEntity, TEntityId> Entities => ManagedEntities;
      }
   }
}
