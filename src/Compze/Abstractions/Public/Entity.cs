using System;
using System.Diagnostics;
using Compze.Utilities.Contracts;

namespace Compze.Core.Public;

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
public class Entity<TEntity, TKey> : IEquatable<TEntity>, IEntity<TKey>
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