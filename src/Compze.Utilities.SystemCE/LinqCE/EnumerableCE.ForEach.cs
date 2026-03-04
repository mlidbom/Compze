using JetBrains.Annotations;
using Compze.Contracts;

// ReSharper disable PossibleMultipleEnumeration

namespace Compze.Utilities.SystemCE.LinqCE;

/// <summary/>
public static partial class EnumerableCE
{
   /// <summary>
   /// Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/>.
   /// </summary>
   public static unit ForEach<TSource, TReturn>(this IEnumerable<TSource> source, [InstantHandle]Func<TSource, TReturn> action) => unit.Invoke(() =>
   {
      Argument.NotNull2(source, action);
      foreach(var item in source)
      {
         action(item);
      }
   });

   /// <summary>
   /// Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/>.
   /// </summary>
   public static unit ForEach<T>(this IEnumerable<T> source, [InstantHandle] Action<T> action) => unit.Invoke(() =>
   {
      Argument.NotNull2(source, action);

      foreach(var item in source)
      {
         action(item);
      }
   });

   /// <summary>
   /// Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/>.
   /// </summary>
   public static unit ForEach<T>(this IEnumerable<T> source, [InstantHandle]Action<T, int> action) => unit.Invoke(() =>
   {
      Argument.NotNull2(source, action);

      var index = 0;
      foreach(var item in source)
      {
         action(item, index++);
      }
   });
}
