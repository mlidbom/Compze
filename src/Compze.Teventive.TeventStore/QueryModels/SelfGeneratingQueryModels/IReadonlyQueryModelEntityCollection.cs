using System.Diagnostics.CodeAnalysis;

namespace Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

public interface IReadonlyQueryModelEntityCollection<TEntity, in TEntityId> : IEnumerable<TEntity> where TEntity : class
{
   IReadOnlyList<TEntity> InCreationOrder { get; }
   bool TryGet(TEntityId id, [NotNullWhen(true)]out TEntity? component);
   bool Contains(TEntityId id);
   TEntity Get(TEntityId id);
   TEntity this[TEntityId id] { get; }
}