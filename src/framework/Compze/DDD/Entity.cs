using System;
using System.Diagnostics;
using Compze.Contracts;

namespace Compze.DDD;

/// <summary>
/// Base class for any class that considers equality to be that the Ids for two instances are the same.
/// 
/// It provides implementations of  <see cref="object.Equals(object)"/>, <see cref="object.GetHashCode"/>, and <see cref="IEquatable{T}"/>.
/// 
/// Equals is implemented as: return <code>!ReferenceEquals(null, other) &amp;&amp; other.Id.Equals(Id)</code>
/// the operators simply uses Equals.
/// 
/// </summary>
[DebuggerDisplay("{" + nameof(ToString) + "()}")]
public class Entity<TEntity, TKey> : IEquatable<TEntity>, IHasPersistentIdentity<TKey>
   where TEntity : Entity<TEntity, TKey>
   where TKey: struct
{
   ///<summary>Construct an instance with <param name="id"> as the <see cref="Id"/></param>.</summary>
   protected Entity(TKey id) => _id = id;

   TKey _id;

   /// <inheritdoc />
   public virtual TKey Id
   {
      get => Assert.Result.ReturnNotDefault(_id);
      private set => _id = Assert.Argument.ReturnNotDefault(value);
   }

   ///<summary>Sets the id of the instance. Should probably never be used except by infrastructure code.</summary>
   [Obsolete("Should probably never be used except by infrastructure code.")]
   protected void SetIdBeVerySureYouKnowWhatYouAreDoing(TKey id) => Id = id;

   ///<summary>Gets the id of the instance bypassing contract validation. Should probably never be used except by infrastructure code.</summary>
   [Obsolete("Should probably never be used except by infrastructure code.")]
#pragma warning disable CA1024 //No matter what the analyzer might think, this is not a good property.
   protected TKey GetIdBypassContractValidation() => _id;
#pragma warning restore CA1024

   /// <summary>
   /// Implements equals using persistent reference semantics.
   /// If two instances have the same Id, Equals will return true.
   /// </summary>
   public virtual bool Equals(TEntity? other) => other is not null && other.Id.Equals(Id);

   /// <summary>
   /// Implements equals using persistent reference semantics.
   /// If two instances have the same Id, Equals will return true.
   /// </summary>
   public override bool Equals(object? obj) => obj is TEntity actualOther && Equals(actualOther);

   /// <inheritdoc />
   public override int GetHashCode() => Id.GetHashCode();

   ///<summary>True if both instances have the same ID</summary>
   public static bool operator ==(Entity<TEntity, TKey>? lhs, Entity<TEntity, TKey>? rhs)
   {
      if (ReferenceEquals(lhs, rhs))
      {
         return true;
      }

      return lhs is not null && lhs.Equals(rhs);
   }

   ///<summary>True if both instances do not have the same ID</summary>
   public static bool operator !=(Entity<TEntity, TKey> lhs, Entity<TEntity, TKey> rhs) => !(lhs == rhs);

   ///<summary>Returns a string similar to: MyType:MyId</summary>
   public override string ToString() => $"{GetType().Name}:{Id}";
}

/// <summary>
/// Simple base class for Entities that ensures a correct identity based <see cref="object.Equals(object)"/>, <see cref="object.GetHashCode"/>, and <see cref="IEquatable{T}"/>.
/// 
/// This class uses <see cref="Guid"/>s as Ids because it is the only built-in .Net type the developers are
/// aware of which can, in practice, guarantee for a system that an PersistentEntity will have a globally unique immutable identity
/// from the moment of instantiation and through any number of persisting-loading cycles. That in turn is an
/// absolute requirement for a correct implementation of <see cref="object.Equals(object)"/>,
/// <see cref="object.GetHashCode"/>, and <see cref="IEquatable{TEntity}"/>.
/// </summary>
[DebuggerDisplay("{GetType().Name} Id={Id}")]
public class Entity<TEntity> : Entity<TEntity, Guid>, IPersistentEntity<Guid>, IEquatable<TEntity> where TEntity : Entity<TEntity>
{
   /// <summary>
   /// Creates an instance using the supplied <paramref name="id"/> as the Id.
   /// </summary>
   protected Entity(Guid id):base(id)
   {
   }

   /// <summary>
   /// Creates a new instance with an automatically generated Id
   /// </summary>
   protected Entity():base(Guid.NewGuid())
   {
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