using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Teventive.Taggregates.BaseClasses._internal;

class EntityCollection<TEntity, TEntityId> : IReadOnlyEntityCollection<TEntity, TEntityId> where TEntity : class where TEntityId : notnull
{
   readonly Dictionary<TEntityId, TEntity> _entities = new();
   readonly List<TEntity> _entitiesInCreationOrder = [];


   public IReadOnlyList<TEntity> InCreationOrder => _entitiesInCreationOrder;

   public bool TryGet(TEntityId id, [NotNullWhen(true)]out TEntity? component) => _entities.TryGetValue(id, out component);
   public bool Contains(TEntityId id) => _entities.ContainsKey(id);
   public TEntity Get(TEntityId id) => _entities[id];
   public TEntity this[TEntityId id] => _entities[id];

   public void Remove(TEntityId id)
   {
      var toRemove = _entities[id];
      _entities.Remove(id);
      _entitiesInCreationOrder.Remove(toRemove);
   }

   ///<summary>Each entity, paired with its id, in the order the entities were created.</summary>
   internal IReadOnlyList<KeyValuePair<TEntityId, TEntity>> InCreationOrderWithTheirIds
   {
      get
      {
         var idOf = new Dictionary<TEntity, TEntityId>(ReferenceEqualityComparer.Instance);
         foreach(var (id, entity) in _entities) idOf.Add(entity, id);
         return [.._entitiesInCreationOrder.Select(entity => new KeyValuePair<TEntityId, TEntity>(idOf[entity], entity))];
      }
   }

   public void Add(TEntity entity, TEntityId id)
   {
      _entities.Add(id, entity);
      _entitiesInCreationOrder.Add(entity);
   }

   public IEnumerator<TEntity> GetEnumerator() => _entitiesInCreationOrder.GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => _entitiesInCreationOrder.GetEnumerator();
}