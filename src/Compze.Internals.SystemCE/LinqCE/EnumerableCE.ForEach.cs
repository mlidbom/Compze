using JetBrains.Annotations;
using Compze.Contracts;

// ReSharper disable PossibleMultipleEnumeration

namespace Compze.Internals.SystemCE.LinqCE;

/// <summary/>
public static partial class EnumerableCE
{
   /// <summary>Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/> and returns the original sequence.</summary>
   public static IEnumerable<TSource> ForEach<TSource, TReturn>(this IEnumerable<TSource> source, [InstantHandle] Func<TSource, TReturn> action) =>
      Argument.NotNull2(source, action)
              ._(() => source._tap(it =>
               {
                  foreach(var item in it)
                  {
                     action(item);
                  }
               }));

   /// <summary>Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/> and returns the original sequence.</summary>
   public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, [InstantHandle] Action<T> action) =>
      Argument.NotNull2(source, action)
              ._(() => source._tap(it =>
               {
                  foreach(var item in it)
                  {
                     action(item);
                  }
               }));

   /// <summary>Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/> and returns the original sequence.</summary>
   public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, [InstantHandle] Action<T, int> action) =>
      Argument.NotNull2(source, action)
              ._(() => source._tap(it =>
               {
                  var index = 0;
                  foreach(var item in it)
                  {
                     action(item, index++);
                  }
               }));
}
