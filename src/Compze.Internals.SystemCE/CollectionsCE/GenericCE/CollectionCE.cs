using Compze.Contracts;

namespace Compze.Internals.SystemCE.CollectionsCE.GenericCE;

///<summary>Extensions on <see cref="ICollection{T}"/></summary>
public static class CollectionCE
{
   ///<summary>Remove entries matching the condition from the collection.</summary>
   public static IReadOnlyList<T> RemoveWhere<T>(this ICollection<T> me, Func<T, bool> condition)
   {
      Argument.NotNull2(me, condition);
      var removed = me.Where(condition).ToList();
      removed.ForEach(removeMe => me.Remove(removeMe));
      return removed;
   }
}
