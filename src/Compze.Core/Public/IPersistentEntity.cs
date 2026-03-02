using System;

namespace Compze.Core.Public;

/// <summary>
/// Should be implemented by all persistent entities in the Domain Driven Design sense of the word.
/// 
/// The vital distinction about entities in DDD is that equality is defined by ID,
/// and as such they must guarantee that they have a non-default identity whenever observable by client code.
///
/// IMPORTANT NOTE:
/// This interface, and Compze as a whole, allow ONLY <see cref="Guid"/>s Ids for persistent entities managed by Compze infrastructure.
/// 
/// Any application requiring other types of Ids, such as natural keys, must manage those themselves and store them in another property on the entities.
/// Compze does intend to implement support to make implementing such alternative IDs simpler in the future,
/// not by removing this constraint, but by adding a support for a second identity property that is not required to fulfil the below requirements for the main ID.
///
/// Rationale:
/// First:
/// In distributed system, where clients send tommands to create new
/// Entities, any type that is not guaranteed to be globally unique requires
/// non-trivial coordination between the client and server to ensure uniqueness
/// and that the client will know the ID of the created Entity.
/// This creates unavoidable additional complexity, not only in implementing frameworks,
/// but also in the applications themselves.
/// 
/// Second:
/// <see cref="Guid"/>> guarantees that an ID is globally unique from the moment of creation and through any number of persisting-loading cycles.
/// That in turn is a requirement for a correct implementation of <see cref="object.Equals(object)"/>,
/// <see cref="object.GetHashCode"/>, and <see cref="IEquatable{TEntity}"/>.
///
/// Third:
/// Guids enable guaranteed traceability of entities. Search for a Guid in a log, database, or code, and you
/// are guaranteed to find all references to that entity and ONLY to that entity, a major advantage when debugging.
/// </summary>
public interface IEntity : IEntity<Guid>
{
   EntityId<Guid> IEntity<Guid>.Id => Id;
   new EntityId Id { get; }
}

public interface ITentity : IEntity;