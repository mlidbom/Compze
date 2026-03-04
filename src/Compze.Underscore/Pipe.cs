

// ReSharper disable InconsistentNaming

namespace Compze.Underscore;

/// <summary>
/// Enables chaining method calls in a fluent functional programming style rather than having to use separate lines and temporary variables.
/// Think of these as "missing operators" for .NET types rather than traditional extension methods.
/// 
/// NAMING CONVENTION: All methods use _camelCase naming (e.g. _tap, _then) for two critical reasons:
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

   ///<summary>An alias for <see cref="_tap{T}"/> which declares that your intent is to mutate the instance.</summary>
   public static T _mutate<T>(this T it, Action<T> mutate) => it._tap(mutate);

   ///<summary> Returns <paramref name="value"/>, ignoring the previous value.  Useful for chaining calls where a constant value is needed.</summary>
   public static TResult _<TValue, TResult>(this TValue _, TResult value) => value;

   ///<summary>Invokes <paramref name="func"/>, ignoring the previous value. Useful for chaining calls where the previous result is irrelevant.</summary>
   public static TResult _<TValue, TResult>(this TValue _, Func<TResult> func) => func();

   ///<summary> Executes <paramref name="action"/>, ignoring the previous value, and returns a <see cref="unit"/>.  Useful for chaining statements that return void.</summary>
   public static unit _<TValue>(this TValue _, Action action) => unit.Invoke(action);

   ///<summary> Returns <paramref name="value"/>, ignoring the previous value.  Useful for chaining calls where a constant value is needed.</summary>
   public static TResult _then<TValue, TResult>(this TValue _, TResult value) => value;

   ///<summary>Invokes <paramref name="func"/>, ignoring the previous value. Useful for chaining calls where the previous result is irrelevant.</summary>
   public static TResult _then<TValue, TResult>(this TValue _, Func<TResult> func) => func();

   ///<summary> Executes <paramref name="action"/>, ignoring the previous value, and returns a <see cref="unit"/>.  Useful for chaining statements that return void.</summary>
   public static unit _then<TValue>(this TValue _, Action action) => unit.Invoke(action);

   ///<summary>Mutates <paramref name="it"/> using <paramref name="mutate"/> and returns <paramref name="it"/></summary>
   public static async Task<T> _mutateAsync<T>(this T it, Func<T, Task> mutate)
   {
      await mutate(it).ConfigureAwait(false);
      return it;
   }
}
