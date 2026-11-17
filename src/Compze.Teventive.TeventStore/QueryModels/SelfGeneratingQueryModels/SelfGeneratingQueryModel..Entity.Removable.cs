using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TTaggregateTevent : class, ITaggregateTevent
{
   ///<summary>An entity that a <typeparamref name="TEntityRemovedTevent"/> removes from its collection, once the tevent has reached the entity's appliers.</summary>
   public abstract class Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter> :
      Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>
      where TEntityId : struct
      where TEntityTevent : class, TTaggregateTevent
      where TEntityCreatedTevent : TEntityTevent
      where TEntityRemovedTevent : TEntityTevent
      where TEntity : Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>
      where TTeventEntityIdGetter : IGetTaggregateEntityTeventEntityId<TEntityTevent, TEntityId>
   {
      ///<summary>Declares the tevent appliers of <typeparamref name="TEntity"/>, as the entity's do, letting its <typeparamref name="TEntityRemovedTevent"/> reach no applier:<br/>
      /// its collection removes it once the tevent has reached any it has.</summary>
      protected new static TeventRouting<TEntity, TEntityTevent> DeclareAppliers(Action<TeventRouting<TEntity, TEntityTevent>.Declaration> declare) =>
         DeclareEntityAppliers(it =>
         {
            it.IgnoreUnhandled<TEntityRemovedTevent>();
            declare(it);
         });

      ///<summary>A collection of the entities of <paramref name="parent"/>, the query model or component whose routing routes their tevents to it.</summary>
      public new static ICollectionManager CreateSelfManagingCollection(ITeventRouted parent) => new CollectionManager(parent);

      new class CollectionManager(ITeventRouted parent) : Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>.CollectionManager(parent)
      {
         protected override bool Removes(TEntityTevent tevent) => tevent is TEntityRemovedTevent;
      }
   }
}
