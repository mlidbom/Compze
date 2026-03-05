using System.Diagnostics.CodeAnalysis;
using Compze.Contracts;
using Compze.Underscore;

namespace Compze.Threading;

#pragma warning disable CA1002 // Utility extension methods returning List by design for copy-and-add pattern
static class ReadonlyCollections
{
   public static Dictionary<TKey, TValue> AddToCopy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> @this, TKey key, TValue value) where TKey : notnull => new(@this) { { key, value } };

   public static Dictionary<TKey, TValue> AddRangeToCopy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> @this, IEnumerable<KeyValuePair<TKey, TValue>> range) where TKey : notnull =>
      new Dictionary<TKey, TValue>(@this)._mutate(me => me.AddRange(range));

   public static List<T> AddToCopy<T>(this IReadOnlyList<T> @this, T item) => [..@this, item];

   public static HashSet<T> AddToCopy<T>(this IReadOnlySet<T> @this, T item) => [..@this, item];

   public static List<T> AddRangeToCopy<T>(this IReadOnlyList<T> @this, IEnumerable<T> items) =>
      new List<T>(@this)._mutate(me => me.AddRange(items));

   [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
   static void AddRange<T>(this ICollection<T> me, IEnumerable<T> toAdd) => Contract.Argument.NotNull2(me, toAdd)._(() =>
   {
      foreach(var it in toAdd)
      {
         me.Add(it);
      }
   });
}
