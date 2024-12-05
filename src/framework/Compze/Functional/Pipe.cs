using System;
using System.Threading.Tasks;
using Compze.SystemCE.ThreadingCE.TasksCE;

// ReSharper disable InconsistentNaming

namespace Compze.Functional;

///<summary>Provides the ability to chain method calls rather than having to use separate lines and temporary variables.</summary>
static class Pipe
{
   ///<summary>Takes the first value, applies <see cref="transform"/> and return the resulting value.</summary>
   public static TResult select<TValue, TResult>(this TValue it, Func<TValue, TResult> transform) => transform(it);

   ///<summary> Returns <paramref name="value"/>, ignoring the previous value.  Useful for chaining calls where a constant value is needed.</summary>
   public static TResult then<TValue, TResult>(this TValue _, TResult value) => value;

   ///<summary>Invokes <paramref name="func"/>, ignoring the previous value. Useful for chaining calls where the previous result is irrelevant.</summary>
   public static TResult then<TValue, TResult>(this TValue _, Func<TResult> func) => func();

   ///<summary> Executes <paramref name="action"/>, ignoring the previous value, and returns a <see cref="Unit"/>.  Useful for chaining statements that return void.</summary>
   public static Unit then<TValue>(this TValue _, Action action) => Unit.From(action);

   ///<summary> Disposes <paramref name="it"/> after applying <paramref name="transform"/> and returning the resulting value.</summary>
   internal static TResult use<TIDisposableValue, TResult>(this TIDisposableValue it, Func<TIDisposableValue, TResult> transform) where TIDisposableValue : IDisposable
   {
      using(it)
      {
         return transform(it);
      }
   }

   ///<summary>Mutates <paramref name="it"/> using <paramref name="mutate"/> and returns <paramref name="it"/></summary>
   public static T mutate<T>(this T it, Action<T> mutate)
   {
      mutate(it);
      return it;
   }

   ///<summary>Mutates <paramref name="it"/> using <paramref name="mutate"/> and returns <paramref name="it"/></summary>
   public static async Task<T> mutateAsync<T>(this T it, Func<T, Task> mutate)
   {
      await mutate(it).CaF();
      return it;
   }
}
