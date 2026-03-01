using System;
using System.Diagnostics;
using Compze.Contracts;
using Compze.Underscore;

namespace Compze.Core.Public;

/// <summary>
/// Base class for any class that considers equality to be that the Ids for two instances are the same,
/// this includes transient entities that exists only in memory and thus this class does not carry the <see cref="Guid"/> only
/// Ids requirement of <see cref="IEntity"/>.
/// 
/// It provides implementations of  <see cref="object.Equals(object)"/>, <see cref="object.GetHashCode"/>, and <see cref="IEquatable{T}"/>.
/// 
/// Equals is implemented as: return <code>!ReferenceEquals(null, other) &amp;&amp; other.Id.Equals(Id)</code>
/// the operators simply uses Equals.
/// 
/// </summary>
[DebuggerDisplay("{" + nameof(ToString) + "()}")]
public class Entity<TEntity, TKey> : IEntity<TKey>
   where TEntity : Entity<TEntity, TKey>
   where TKey: IEquatable<TKey>
{
   ///<summary>Construct an instance with <param name="id"> as the <see cref="Id"/></param>.</summary>
   protected Entity(EntityId<TKey> id) => _id = id;

   EntityId<TKey> _id;

   /// <inheritdoc />
   public virtual EntityId<TKey> Id
   {
      get => _id._assert(it => !it.Value.Equals(default));
      [Obsolete(ObsoleteMessage.ForInternalUseOnly)]
      protected set => _id = Contract.Argument.NotNull(value)._then(value);
   }

   ///<summary>Gets the id of the instance bypassing contract validation. Should probably never be used except by infrastructure code.</summary>
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)]
#pragma warning disable CA1024 //No matter what the analyzer might think, this is not a good property.
   protected EntityId<TKey> GetIdBypassContractValidation() => _id;
#pragma warning restore CA1024


   ///<summary>Returns a string similar to: MyType:MyId</summary>
   public override string ToString() => $"{GetType().Name}:{Id}";
}