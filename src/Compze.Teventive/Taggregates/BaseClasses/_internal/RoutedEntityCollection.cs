using System.Collections;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.BaseClasses._private;

namespace Compze.Teventive.Taggregates.BaseClasses._internal;

///<summary>What every collection of entities that a <see cref="TeventRouting"/> routes to does with a tevent of its entities: a created tevent adds a<br/>
/// new entity, every tevent reaches the entity whose id it carries, and a removed tevent then takes the entity out, once it has reached the<br/>
/// entity's appliers. The order is load-bearing: an entity's appliers find it in its collection, and a removed entity has applied its removal.</summary>
abstract class RoutedEntityCollection<TEntity, TEntityId, TEntityTevent> : ITeventRoutedPart<TEntityTevent>, IEntityCollectionState
   where TEntity : class, ITeventRoutedPart<TEntityTevent>
   where TEntityId : struct
   where TEntityTevent : class, ITevent
{
   ///<summary>The entities, by id and in the order they were created.</summary>
   internal EntityCollection<TEntity, TEntityId> ManagedEntities { get; } = [];

   IEnumerable IEntityCollectionState.EntitiesWithTheirIdsInCreationOrder => ManagedEntities.InCreationOrderWithTheirIds;

   ///<summary>The id of the entity <paramref name="tevent"/> belongs to.</summary>
   protected abstract TEntityId IdOf(TEntityTevent tevent);

   ///<summary>Whether <paramref name="tevent"/> creates an entity.</summary>
   protected abstract bool Creates(TEntityTevent tevent);

   ///<summary>A new entity, for a created tevent to reach.</summary>
   protected abstract TEntity CreateEntity();

   ///<summary>Whether <paramref name="tevent"/> removes its entity. None does, in a collection whose entities can't be removed.</summary>
   protected virtual bool Removes(TEntityTevent tevent) => false;

#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
   void ITeventRoutedPart<TEntityTevent>.DispatchInternal(IPublisherTevent<ITevent> wrapped)
   {
      var tevent = (TEntityTevent)wrapped.Tevent;
      var id = IdOf(tevent);
      if(Creates(tevent)) ManagedEntities.Add(CreateEntity(), id);
      ManagedEntities[id].DispatchInternal(wrapped);
      if(Removes(tevent)) ManagedEntities.Remove(id);
   }
#pragma warning restore CS0618
}
