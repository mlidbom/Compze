

// ReSharper disable InconsistentNaming

using Compze.SystemCE;

namespace Compze.Underscore;

/// <summary>
/// Enables chaining method calls in a fluent functional programming style rather than having to use separate lines and temporary variables.
/// Think of these as "missing operators" for .NET types rather than traditional extension methods.
/// 
/// NAMING CONVENTION: All methods use _camelCase naming (e.g. _tap, __) for two critical reasons:
/// 
/// 1. VISUAL DISTINCTION: The underscore prefix makes these instantly recognizable
///    as language-like functional operators, distinct from both standard PascalCase methods.
/// 
/// 2. COLLISION AVOIDANCE: Since these are extensions on ALL types,
///    avoiding name conflicts with existing methods is vital.
///    The _camelCase convention provides virtually zero collision risk.
/// </summary>
public static class Pipe
{
   ///<summary>passes <paramref name="it"/> to <paramref name="func"/> and returns the result. It is the pipe forward operator that is missing in C#. If you start using it, soon ._( will become the missing operator in your mind.</summary>
   public static TResult _<TThis, TResult>(this TThis it, Func<TThis, TResult> func) => func(it);

   ///<summary>Passes <paramref name="it"/> to <paramref name="tap"/> and returns <paramref name="it"/></summary>
   public static T _tap<T>(this T it, Action<T> tap)
   {
      tap(it);
      return it;
   }

   ///<summary>An alias for <see cref="_tap{T}"/> which declares that your intent is to mutate <paramref name="it"/>.</summary>
   public static T _mutate<T>(this T it, Action<T> mutate) => it._tap(mutate);

   ///<summary> Returns <paramref name="value"/>, ignoring the previous value.</summary>
   public static TResult __<TValue, TResult>(this TValue _, TResult value) => value;

   ///<summary>Invokes <paramref name="func"/>, ignoring the previous value.</summary>
   public static TResult __<TValue, TResult>(this TValue _, Func<TResult> func) => func();

   ///<summary> Executes <paramref name="action"/>, and returns <see cref="Unit.Value"/>, ignoring the previous value.</summary>
   public static Unit __<TValue>(this TValue _, Action action) => Unit.Invoke(action);

   ///<summary>Mutates <paramref name="it"/> using <paramref name="mutate"/> and returns <paramref name="it"/></summary>
   public static async Task<T> _mutateAsync<T>(this T it, Func<T, Task> mutate)
   {
      await mutate(it).ConfigureAwait(false);
      return it;
   }
}
