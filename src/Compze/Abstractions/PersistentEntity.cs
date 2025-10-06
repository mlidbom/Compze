using System;
using System.Diagnostics;
using Compze.DDD.Abstractions;

namespace Compze.Abstractions;

/// <summary>
/// Simple base class for <see cref="IPersistentEntity"/>> that ensures a correct identity based <see cref="object.Equals(object)"/>, <see cref="object.GetHashCode"/>, and <see cref="IEquatable{T}"/>.
/// </summary>
// ReSharper disable once NotResolvedInText
[DebuggerDisplay("{GetType().Name} Id={Id}")]
public class PersistentEntity<TEntity> : Entity<TEntity, Guid>, IPersistentEntity, IEquatable<TEntity> where TEntity : PersistentEntity<TEntity>
{
    /// <summary>Creates an instance using the supplied <paramref name="id"/> as the Id.</summary>
    protected PersistentEntity(Guid id):base(id)
    {
    }

    /// <summary>Creates a new instance with an automatically generated Id</summary>
    protected PersistentEntity():base(Guid.NewGuid())
    {
    }

    ///<summary>True if both instances have the same ID</summary>
    public static bool operator ==(PersistentEntity<TEntity>? lhs, PersistentEntity<TEntity>? rhs)
    {
        if (ReferenceEquals(lhs, rhs))
        {
            return true;
        }

        return lhs is not null && lhs.Equals(rhs);
    }

    ///<summary>True if both instances do not have the same ID</summary>
    public static bool operator !=(PersistentEntity<TEntity> lhs, PersistentEntity<TEntity> rhs) => !(lhs == rhs);

    public new bool Equals(TEntity? other) => base.Equals(other);
    // ReSharper disable once RedundantOverriddenMember If I remove this I get another worse warning...
    public override bool Equals(object? obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();
}
