using System.Diagnostics;
using Compze.Contracts;

namespace Compze.Abstractions.Public;

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
      protected set => _id = Argument.NotNull(value).__(value);
   }

   ///<summary>Returns a string similar to: MyType:MyId</summary>
   public override string ToString() => $"{GetType().Name}:{Id}";
}