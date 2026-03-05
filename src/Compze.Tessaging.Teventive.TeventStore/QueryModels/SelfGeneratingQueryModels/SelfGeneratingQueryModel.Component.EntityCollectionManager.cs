using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
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
      public class QueryModelEntityCollectionManager<TParent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
         : IQueryModelEntityCollectionManager<TEntity, TEntityId>
         where TEntityId : notnull
         where TEntityTevent : class, TTaggregateTevent
         where TEntityCreatedTevent : TEntityTevent
         where TEntity : Component<TEntity, TEntityTevent>
         where TEntityTeventIdGetterSetter : IGetTaggregateEntityTeventEntityId<TEntityTevent, TEntityId>
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
