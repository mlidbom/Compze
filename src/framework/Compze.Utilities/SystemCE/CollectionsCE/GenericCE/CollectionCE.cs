using System;
using System.Collections.Generic;
using System.Linq;
using Compze.SystemCE.LinqCE;
using static Compze.Contracts.Assert;

namespace Compze.SystemCE.CollectionsCE.GenericCE;

///<summary>Extensions on <see cref="ICollection{T}"/></summary>
static class CollectionCE
{
   ///<summary>Remove entries matching the condition from the collection.</summary>
   public static IReadOnlyList<T> RemoveWhere<T>(this ICollection<T> me, Func<T, bool> condition)
   {
      Argument.NotNull(me).NotNull(condition);
      var removed = me.Where(condition).ToList();
      removed.ForEach(removeMe => me.Remove(removeMe));
      return removed;
   }

   ///<summary>Add all instances in <param name="toAdd"> to the collection <param name="me"></param>.</param></summary>
   public static void AddRange<T>(this ICollection<T> me, IEnumerable<T> toAdd)
   {
      Argument.NotNull(me).NotNull(toAdd);
      toAdd.ForEach(me.Add);
   }
}