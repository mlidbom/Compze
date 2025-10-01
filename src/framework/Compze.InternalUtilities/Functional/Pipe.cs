using System;
using System.Threading.Tasks;
using Compze.SystemCE.ThreadingCE.TasksCE;

// ReSharper disable InconsistentNaming

namespace Compze.Functional;

/// <summary>
/// Enables chaining method calls in a fluent functional programming style rather than having to use separate lines and temporary variables.
/// Think of these as "missing operators" for .NET types rather than traditional extension methods.
/// 
/// NAMING CONVENTION: All methods use lowercase naming breaking .NET conventions for two critical reasons:
/// 
/// 1. VISUAL DISTINCTION: Instantly recognizable as language-like features,
///    not domain methods - similar to F#'s pipe-forward (|>) operator
///    and LINQ query keywords (where, select).
/// 
/// 2. COLLISION AVOIDANCE: Since these are extensions on ALL types,
///    avoiding name conflicts with existing methods is vital.
///    Lowercase naming is the most effective strategy we found.
/// </summary>
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
