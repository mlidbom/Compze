using System;
using System.Collections.Generic;
using Compze.Utilities.Contracts;
using Compze.Functional;

namespace Compze.Utilities.SystemCE.ThreadingCE;

#pragma warning disable CA1002 // Utility extension methods returning List by design for copy-and-add pattern
public static class ReadonlyCollectionsTE
{
   public static Dictionary<TKey, TValue> AddToCopy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> @this, TKey key, TValue value) where TKey : notnull => new(@this) { { key, value } };

   public static Dictionary<TKey, TValue> AddRangeToCopy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> @this, IEnumerable<KeyValuePair<TKey, TValue>> range) where TKey : notnull =>
      new Dictionary<TKey, TValue>(@this)._mutate(me => AddRange(me, range));

   public static List<T> AddToCopy<T>(this IReadOnlyList<T> @this, T item) => [..@this, item];

   public static HashSet<T> AddToCopy<T>(this IReadOnlySet<T> @this, T item) => [..@this, item];

   public static List<T> AddRangeToCopy<T>(this IReadOnlyList<T> @this, IEnumerable<T> items) =>
      new List<T>(@this)._mutate(me => me.AddRange(items));

   public static T[] AddToCopy<T>(this T[] @this, T itemToAdd)
   {
      var copy = new T[@this.Length + 1];
      Array.Copy(@this, copy, @this.Length);
      copy[^1] = itemToAdd;
      return copy;
   }

   public static void AddRange<T>(this ICollection<T> me, IEnumerable<T> toAdd)
   {
      Assert.Argument.NotNull(me).NotNull(toAdd);
      foreach(var it in toAdd)
      {
         me.Add(it);
      }
   }
}
