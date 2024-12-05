using System.Collections.Generic;
using Compze.Contracts.Deprecated;
using Compze.SystemCE.LinqCE;

namespace Compze.SystemCE.CollectionsCE.GenericCE;

/// <summary>A collection of extensions to work with <see cref="HashSet{T}"/></summary>
static class HashSetCE
{
   ///<summary>
   /// Removes all of the items in the supplied enumerable from the set.
   /// Simply forwards to ExceptWith but providing a name that is not utterly unreadable </summary>
   public static void RemoveRange<T>(this ISet<T> me, IEnumerable<T> toRemove)
   {
      Contracts.Assert.Argument.NotNull(me).NotNull(toRemove);
      me.ExceptWith(toRemove);
   }

   ///<summary>Adds all the supplied <paramref name="toAdd"/> instances to the set.</summary>
   public static void AddRange<T>(this ISet<T> me, IEnumerable<T> toAdd)
   {
      Contracts.Assert.Argument.NotNull(me).NotNull(toAdd);
      toAdd.ForEach(me.Add);
   }

   public static HashSetCEx<TItem> ToSetCE<TItem>(this IEnumerable<TItem> @this) => new(@this);
}