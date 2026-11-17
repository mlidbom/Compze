using System.Diagnostics.CodeAnalysis;

namespace Compze.Teventive.Taggregates.BaseClasses;

public interface IReadOnlyEntityCollection<TEntity, in TEntityId> : IEnumerable<TEntity> where TEntity : class
{
   IReadOnlyList<TEntity> InCreationOrder { get; }
   // ReSharper disable once UnusedMember.Global todo:write test
   bool TryGet(TEntityId id, [NotNullWhen(true)]out TEntity? component);
   bool Contains(TEntityId id);
   TEntity Get(TEntityId id);
   TEntity this[TEntityId id] { get; }
}

public interface IEntityCollectionManager<TEntity, in TEntityId,in TEntityTevent, in TEntityTeventImplementation, in TEntityCreatedTevent>
   where TEntity : class
   where TEntityTevent : class
   where TEntityCreatedTevent : TEntityTevent
{
   IReadOnlyEntityCollection<TEntity, TEntityId> Entities { get; }
   TEntity AddByPublishing<TCreationTevent>(TCreationTevent creationTevent) where TCreationTevent : TEntityTeventImplementation, TEntityCreatedTevent;

   ///<summary>Adds a new entity with the id <paramref name="id"/>, publishing nothing, for the restore of its taggregate from a snapshot to give<br/>
   /// the rest of its state, as that restore does a component's. Only while the taggregate is being restored: from its <c>RestoreIdentity</c><br/>
   /// until its restoring code returns to the taggregate store restoring it, or else until its first publish. At any other time an entity is<br/>
   /// added by publishing its created tevent, with <see cref="AddByPublishing{TCreationTevent}"/>.</summary>
   ///<remarks>review-naming: a placeholder (jastudio-rep-opus, 2026-11-18).</remarks>
   TEntity AddRestored(TEntityId id);
}