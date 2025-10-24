using System;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Functional;

/// <summary>
/// Enables chaining method calls in a fluent functional programming style rather than having to use separate lines and temporary variables.
/// Think of these as "missing operators" for .NET types rather than traditional extension methods.
/// 
/// NAMING CONVENTION: All methods use lowercase naming breaking .NET conventions for two critical reasons:
/// 
/// 1. VISUAL DISTINCTION: Instantly recognizable as language-like features,
/// 
/// 2. COLLISION AVOIDANCE: Since these are extensions on ALL types,
///    avoiding name conflicts with existing methods is vital.
///    Lowercase naming is the most effective strategy we found.
/// </summary>
public static class Pipe
{
   ///<summary>Takes the first value, applies <see cref="transform"/> and return the resulting value.</summary>
   public static TResult select<TValue, TResult>(this TValue it, Func<TValue, TResult> transform) => transform(it);

   ///<summary> Returns <paramref name="value"/>, ignoring the previous value.  Useful for chaining calls where a constant value is needed.</summary>
   public static TResult then<TValue, TResult>(this TValue _, TResult value) => value;

   ///<summary>Invokes <paramref name="func"/>, ignoring the previous value. Useful for chaining calls where the previous result is irrelevant.</summary>
   public static TResult then<TValue, TResult>(this TValue _, Func<TResult> func) => func();

   ///<summary> Executes <paramref name="action"/>, ignoring the previous value, and returns a <see cref="unit"/>.  Useful for chaining statements that return void.</summary>
   public static unit then<TValue>(this TValue _, Action action) => Functional.unit.From(action);

   ///<summary>Get unit.Value from any value in order to easily return unit anywhere.</summary>
   public static unit unit<T>(this T _) => Functional.unit.Value;
   
   ///<summary>passes <paramref name="it"/> to <paramref name="func"/> and returns the result. It is the pipe forward operator that is missing in C#. If you start using it, soon ._( will become the missing operator in your mind.</summary>
   public static TResult _<TThis, TResult>(this TThis it, Func<TThis, TResult> func) => func(it);

   ///<summary>Mutates <paramref name="it"/> using <paramref name="mutate"/> and returns <paramref name="it"/></summary>
   public static T mutate<T>(this T it, Action<T> mutate)
   {
      mutate(it);
      return it;
   }

   ///<summary>Throws Exception if <paramref name="predicate"/> returns false when applied to <paramref name="it"/> otherwise returns <paramref name="it"/></summary>
   public static T assert<T>(this T it, Predicate<T> predicate, Func<T, string> messageFactory) =>
      it.assert(predicate, () => new Exception(messageFactory(it)));


   ///<summary>Throws <paramref name="exceptionFactory"/>() if <paramref name="predicate"/> returns false when applied to <paramref name="it"/> otherwise returns <paramref name="it"/></summary>
   public static T assert<T>(this T it, Predicate<T> predicate, Func<Exception> exceptionFactory)
   {
      if (!predicate(it)) throw exceptionFactory();
      return it;
   }

   ///<summary>Mutates <paramref name="it"/> using <paramref name="mutate"/> and returns <paramref name="it"/></summary>
   public static async Task<T> mutateAsync<T>(this T it, Func<T, Task> mutate)
   {
      await mutate(it).ConfigureAwait(false);
      return it;
   }
}
