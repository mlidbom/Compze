﻿using System;
using System.Collections.Generic;
using Compze.Functional;

namespace Compze.SystemCE.CollectionsCE.GenericCE;

static class ReadonlyCollectionsCE
{
   public static Dictionary<TKey, TValue> AddToCopy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> @this, TKey key, TValue value) where TKey : notnull => new(@this) {{key, value}};

   public static Dictionary<TKey, TValue> AddRangeToCopy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> @this, IEnumerable<KeyValuePair<TKey, TValue>> range) where TKey : notnull =>
      new Dictionary<TKey, TValue>(@this).mutate(me => me.AddRange(range));

   public static List<T> AddToCopy<T>(this IReadOnlyList<T> @this, T item) => [..@this, item];

   public static HashSet<T> AddToCopy<T>(this IReadOnlySet<T> @this, T item) => [..@this, item];

   public static List<T> AddRangeToCopy<T>(this IReadOnlyList<T> @this, IEnumerable<T> items) =>
      new List<T>(@this).mutate(me => me.AddRange(items));

   public static T[] AddToCopy<T>(this T[] @this, T itemToAdd)
   {
      var copy = new T[@this.Length + 1];
      Array.Copy(@this, copy, @this.Length);
      copy[^1] = itemToAdd;
      return copy;
   }
}