using System;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Contracts;

/// <summary>
/// Pipeline-friendly assertion extension methods.
/// Uses the same _camelCase naming convention as Compze.Functional's Pipe operators
/// so they blend naturally in pipelines.
/// </summary>
public static class PipeAssert
{
   extension<T>(T @this)
   {
      ///<summary>Throws <see cref="AssertionFailedException"/> if <paramref name="predicate"/> returns false when applied to <paramref name="this"/>. The exception message includes the predicate source expression and the value.</summary>
      public T _assert(Predicate<T> predicate, [CallerArgumentExpression(nameof(predicate))] string? predicateExpression = null)
      {
         if(!predicate(@this))
            throw new AssertionFailedException($"Assertion failed: {predicateExpression} (value: {@this})");
         return @this;
      }

      ///<summary>Throws <see cref="AssertionFailedException"/> with message from <paramref name="messageFactory"/> if <paramref name="predicate"/> returns false when applied to <paramref name="this"/>. The factory is only invoked on failure, avoiding allocation in the success path.</summary>
      public T _assert(Predicate<T> predicate, Func<T, string> messageFactory) =>
         @this._assert(predicate, () => new AssertionFailedException(messageFactory(@this)));

      ///<summary>Throws <paramref name="exceptionFactory"/>() if <paramref name="predicate"/> returns false when applied to <paramref name="this"/> otherwise returns <paramref name="this"/></summary>
      public T _assert(Predicate<T> predicate, Func<Exception> exceptionFactory)
      {
         if(!predicate(@this))
            throw exceptionFactory();
         return @this;
      }
   }
}
