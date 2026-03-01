using System;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

namespace Compze.Functional;

/// <summary>
/// Enables chaining method calls in a fluent functional programming style rather than having to use separate lines and temporary variables.
/// Think of these as "missing operators" for .NET types rather than traditional extension methods.
/// 
/// NAMING CONVENTION: All methods use _camelCase naming (e.g. _tap, _then) for two critical reasons:
/// 
/// 1. VISUAL DISTINCTION: The underscore prefix makes these instantly recognizable
///    as language-like functional operators, distinct from both standard PascalCase methods
///    and _camelCase private fields (which are nouns, not verbs).
/// 
/// 2. COLLISION AVOIDANCE: Since these are extensions on ALL types,
///    avoiding name conflicts with existing methods is vital.
///    The _camelCase convention provides virtually zero collision risk.
/// </summary>
public static class Pipe
{
   extension<TThis>(TThis it)
   {
      ///<summary>passes <paramref name="it"/> to <paramref name="func"/> and returns the result. It is the pipe forward operator that is missing in C#. If you start using it, soon ._( will become the missing operator in your mind.</summary>
      public TResult _<TResult>(Func<TThis, TResult> func) => func(it);

      ///<summary>Passes <paramref name="it"/> to <paramref name="tap"/> and returns <paramref name="it"/></summary>
      public TThis _tap(Action<TThis> tap)
      {
         tap(it);
         return it;
      }

      ///<summary>An alias for <see cref="_tap{T}"/> which declares that your intent is to mutate the instance.</summary>
      public TThis _mutate(Action<TThis> mutate) => it._tap(mutate);

      ///<summary> Returns <paramref name="value"/>, ignoring the previous value.  Useful for things like returning `this` at the end of a pipeline.</summary>
      public TResult _<TResult>(TResult value) => value;

      ///<summary> Returns <paramref name="value"/>, ignoring the previous value.  Useful for things like returning `this` at the end of a pipeline.</summary>
      public TResult _then<TResult>(TResult value) => value;

      ///<summary>Invokes <paramref name="func"/>, ignoring the previous value. Useful for chaining calls where the previous result is irrelevant.</summary>
      public TResult _<TResult>(Func<TResult> func) => func();

      ///<summary>Invokes <paramref name="func"/>, ignoring the previous value. Useful for chaining calls where the previous result is irrelevant.</summary>
      public TResult _then<TResult>(Func<TResult> func) => func();

      ///<summary>Mutates <paramref name="it"/> using <paramref name="mutate"/> and returns <paramref name="it"/></summary>
      public async Task<TThis> _mutateAsync(Func<TThis, Task> mutate)
      {
         await mutate(it).ConfigureAwait(false);
         return it;
      }
   }
}
