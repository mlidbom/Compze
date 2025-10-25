using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;

public interface IReadOnlyEntityCollection<TEntity, in TEntityId> : IEnumerable<TEntity>
{
   IReadOnlyList<TEntity> InCreationOrder { get; }
   // ReSharper disable once UnusedMember.Global todo:write test
   bool TryGet(TEntityId id, [MaybeNullWhen(false)]out TEntity component);
   bool Contains(TEntityId id);
   TEntity Get(TEntityId id);
   TEntity this[TEntityId id] { get; }
}

interface IEntityCollectionManager<TEntity, in TEntityId,in TEntityTevent, in TEntityTeventImplementation, in TEntityCreatedTevent>
   where TEntityTevent : class
   where TEntityCreatedTevent : TEntityTevent
{
   IReadOnlyEntityCollection<TEntity, TEntityId> Entities { get; }
   TEntity AddByPublishing<TCreationTevent>(TCreationTevent creationTevent) where TCreationTevent : TEntityTeventImplementation, TEntityCreatedTevent;
}