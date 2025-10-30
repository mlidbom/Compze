using System;
using System.Diagnostics;

namespace Compze.Core.Public;

/// <summary>
/// Simple base class for <see cref="IEntity"/>> that ensures a correct identity based <see cref="object.Equals(object)"/>, <see cref="object.GetHashCode"/>, and <see cref="IEquatable{T}"/>.
/// </summary>
// ReSharper disable once NotResolvedInText
[DebuggerDisplay("{GetType().Name} Id={Id}")]
public class Entity<TEntity> : Entity<TEntity, Guid>, IEntity, IEquatable<TEntity> where TEntity : Entity<TEntity>
{
    /// <summary>Creates an instance using the supplied <paramref name="id"/> as the Id.</summary>
    protected Entity(Guid id):base(new EntityId(id))
    {
    }

    /// <summary>Creates an instance using the supplied <paramref name="id"/> as the Id.</summary>
    protected Entity(EntityId id):base(id)
    {
    }

    /// <summary>Creates a new instance with an automatically generated Id</summary>
    protected Entity():base(new EntityId())
    {
    }

    EntityId<Guid> IEntity<Guid>.Id => Id;

    public new virtual EntityId Id
    {
       get => (EntityId)base.Id;
       protected set => base.Id = value;
    }

    ///<summary>True if both instances have the same ID</summary>
    public static bool operator ==(Entity<TEntity>? lhs, Entity<TEntity>? rhs)
    {
        if (ReferenceEquals(lhs, rhs))
        {
            return true;
        }

        return lhs is not null && lhs.Equals(rhs);
    }

    ///<summary>True if both instances do not have the same ID</summary>
    public static bool operator !=(Entity<TEntity> lhs, Entity<TEntity> rhs) => !(lhs == rhs);

    public new bool Equals(TEntity? other) => base.Equals(other);
    // ReSharper disable once RedundantOverriddenMember If I remove this I get another worse warning...
    public override bool Equals(object? obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();
}
