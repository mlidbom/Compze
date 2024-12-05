using System;
using System.Collections.Generic;
using Compze.Contracts.Deprecated;
using JetBrains.Annotations;

namespace Compze.SystemCE.LinqCE;

/// <summary/>
public static partial class EnumerableCE
{
   /// <summary>
   /// Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/>.
   /// </summary>
   public static void ForEach<TSource, TReturn>(this IEnumerable<TSource> source, Func<TSource, TReturn> action)
   {
      Contracts.Assert.Argument.NotNull(source).NotNull(action);
      foreach(var item in source)
      {
         action(item);
      }
   }

   /// <summary>
   /// Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/>.
   /// </summary>
   public static void ForEach<T>(this IEnumerable<T> source, [InstantHandle]Action<T> action)
   {
      Contracts.Assert.Argument.NotNull(source).NotNull(action);

      foreach (var item in source)
      {
         action(item);
      }
   }

   /// <summary>
   /// Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/>.
   /// </summary>
   public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
   {
      Contracts.Assert.Argument.NotNull(source).NotNull(action);

      var index = 0;
      foreach(var item in source)
      {
         action(item, index++);
      }
   }
}