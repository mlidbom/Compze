using System;
using System.Diagnostics;
using Compze.Utilities.Contracts;

namespace Compze.DDD.Abstractions;

/// <summary>
/// Base class for any class that considers equality to be that the Ids for two instances are the same,
/// this includes transient entities that exists only in memory and thus this class does not carry the <see cref="Guid"/> only
/// Ids requirement of <see cref="IPersistentEntity"/>.
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
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)]
   protected void SetIdBeVerySureYouKnowWhatYouAreDoing(TKey id) => Id = id;

   ///<summary>Gets the id of the instance bypassing contract validation. Should probably never be used except by infrastructure code.</summary>
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)]
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
/// Simple base class for <see cref="IPersistentEntity"/>> that ensures a correct identity based <see cref="object.Equals(object)"/>, <see cref="object.GetHashCode"/>, and <see cref="IEquatable{T}"/>.
///
/// IMPORTANT NOTE:
/// This class, and Compze as a whole, allows ONLY <see cref="Guid"/>s Ids for persistent entities:
/// Any application requiring other types of Ids for user display, use of id's from other systems etc.
/// must manage those as surrogate keys themselves.
///
/// Rationale:
/// First:
/// It is the only built-in .Net type the developers are aware of which can, in practice, guarantee
/// for a system that a persistent entity will have a globally unique immutable identity
/// from the moment of creation and through any number of persisting-loading cycles. That in turn is an
/// absolute requirement for a correct implementation of <see cref="object.Equals(object)"/>,
/// <see cref="object.GetHashCode"/>, and <see cref="IEquatable{TEntity}"/>.
///
/// Second:
/// In distributed system, where clients send commands to create new
/// Entities, any type but a Guid requires error-prone and complex coordination between the client and
/// server to ensure uniqueness and that the client will know the identity of the created Entity in advance.
/// This creates an explosion of complexity, not only in implementing frameworks,
/// but also in the applications themselves.
///
/// Managing a separate surrogate sequential key does not entail all this complexity.
/// </summary>
// ReSharper disable once NotResolvedInText
[DebuggerDisplay("{GetType().Name} Id={Id}")]
public class PersistentEntity<TEntity> : Entity<TEntity, Guid>, IPersistentEntity, IEquatable<TEntity> where TEntity : PersistentEntity<TEntity>
{
   /// <summary>
   /// Creates an instance using the supplied <paramref name="id"/> as the Id.
   /// </summary>
   protected PersistentEntity(Guid id):base(id)
   {
   }

   /// <summary>
   /// Creates a new instance with an automatically generated Id
   /// </summary>
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