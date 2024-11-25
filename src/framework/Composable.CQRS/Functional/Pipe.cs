using System;

// ReSharper disable InconsistentNaming

namespace Composable.Functional;

///<summary>Provides the ability to chain method calls rather than having to use separate lines and temporary variables.</summary>
static class Pipe
{
   ///<summary>Takes the first value, applies the <see cref="transform"/> and return the resulting value.</summary>
   public static TResult select<TValue, TResult>(this TValue it, Func<TValue, TResult> transform) => transform(it);

   ///<summary>Sometimes you want to continue a method chain but don't care about the previous value. This drops it</summary>
   public static TResult then<TValue, TResult>(this TValue _, Func<TResult> func) => func();

   ///<summary>Sometimes you want to continue a method chain but don't care about the previous value. This drops it</summary>
   public static TResult then<TValue, TResult>(this TValue _, TResult value) => value;

   ///<summary>Enables chaining statements that return void. For when separate lines of code is inconvenient, such as in many lambda expressions.</summary>
   internal static Unit then<TValue>(this TValue _, Action action) => Unit.From(action);

   ///<summary>Use an <see cref="IDisposable"/> resource and return the result.</summary>
   internal static TResult use<TIDisposableValue, TResult>(this TIDisposableValue it, Func<TIDisposableValue, TResult> transform) where TIDisposableValue : IDisposable
   {
      using(it)
      {
         return transform(it);
      }
   }
}
