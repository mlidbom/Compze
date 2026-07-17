using System.Diagnostics.CodeAnalysis;
using Compze.Contracts;

namespace Compze.Internals.SystemCE.CollectionsCE.GenericCE;

#pragma warning disable CA1002 // Utility extension methods returning List by design for copy-and-add pattern
public static class ReadonlyCollectionsCE
{
   public static IReadOnlyList<T> AddToCopy<T>(this IReadOnlyList<T> @this, T item) => [..@this, item];

   public static Dictionary<TKey, TValue> AddToCopy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> @this, TKey key, TValue value) where TKey : notnull => new(@this) { { key, value } };

   ///<summary>Like <see cref="AddToCopy{TKey,TValue}(IReadOnlyDictionary{TKey,TValue},TKey,TValue)"/> but overwrites: the copy holds <paramref name="value"/> under <paramref name="key"/> whether or not the key was already present.</summary>
   public static Dictionary<TKey, TValue> SetInCopy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> @this, TKey key, TValue value) where TKey : notnull => new(@this) { [key] = value };

   ///<summary>The removal counterpart of <see cref="SetInCopy{TKey,TValue}"/>: a copy without <paramref name="key"/> — identical to the original when the key was absent.</summary>
   public static Dictionary<TKey, TValue> RemoveFromCopy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> @this, TKey key) where TKey : notnull => new Dictionary<TKey, TValue>(@this)._mutate(me => me.Remove(key));

   public static Dictionary<TKey, TValue> AddRangeToCopy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> @this, IEnumerable<KeyValuePair<TKey, TValue>> range) where TKey : notnull =>
      new Dictionary<TKey, TValue>(@this)._mutate(me => me.AddRange(range));

   public static HashSet<T> AddToCopy<T>(this IReadOnlySet<T> @this, T item) => [..@this, item];

   public static List<T> AddRangeToCopy<T>(this IReadOnlyList<T> @this, IEnumerable<T> items) =>
      new List<T>(@this)._mutate(me => me.AddRange(items));

   [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
   static void AddRange<T>(this ICollection<T> me, IEnumerable<T> toAdd) => Argument.NotNull2(me, toAdd).__(() =>
   {
      foreach(var it in toAdd)
      {
         me.Add(it);
      }
   });
}
