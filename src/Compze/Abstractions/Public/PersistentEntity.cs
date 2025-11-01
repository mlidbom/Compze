using System;
using System.Diagnostics;

namespace Compze.Core.Public;

/// <summary>
/// Simple base class for <see cref="IEntity"/>> that ensures a correct identity based <see cref="object.Equals(object)"/>, <see cref="object.GetHashCode"/>, and <see cref="IEquatable{T}"/>.
/// </summary>
// ReSharper disable once NotResolvedInText
[DebuggerDisplay("{GetType().Name} Id={Id}")]
public class Entity<TEntity> : Entity<TEntity, Guid>, IEntity where TEntity : Entity<TEntity>
{
   /// <summary>Creates an instance using the supplied <paramref name="id"/> as the Id.</summary>
   protected Entity(Guid id) : base(new EntityId(id)) {}

   /// <summary>Creates an instance using the supplied <paramref name="id"/> as the Id.</summary>
   protected Entity(EntityId id) : base(id) {}

   /// <summary>Creates a new instance with an automatically generated Id</summary>
   protected Entity() : base(new EntityId()) {}

   EntityId<Guid> IEntity<Guid>.Id => Id;

   public new virtual EntityId Id
   {
      get => (EntityId)base.Id;
      [Obsolete(ObsoleteMessage.ForInternalUseOnly)]
      protected set => base.Id = value;
   }
}
