using System;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Contracts;

/// <summary>
/// Pipeline-friendly assertion extension methods.
/// Uses the same _camelCase naming convention as Compze.Functional's Pipe operators
/// so they blend naturally in pipelines.
/// </summary>
public static class PipeAssert
{
   ///<summary>Throws <see cref="AssertionFailedException"/> if <paramref name="predicate"/> returns false when applied to <paramref name="it"/>. The exception message includes the predicate source expression and the value.</summary>
   public static T _assert<T>(this T it, Predicate<T> predicate, [CallerArgumentExpression(nameof(predicate))] string? predicateExpression = null)
   {
      if(!predicate(it))
         throw new AssertionFailedException($"Assertion failed: {predicateExpression} (value: {it})");
      return it;
   }

   ///<summary>Throws <see cref="AssertionFailedException"/> with message from <paramref name="messageFactory"/> if <paramref name="predicate"/> returns false when applied to <paramref name="it"/>. The factory is only invoked on failure, avoiding allocation in the success path.</summary>
   public static T _assert<T>(this T it, Predicate<T> predicate, Func<T, string> messageFactory) =>
      it._assert(predicate, () => new AssertionFailedException(messageFactory(it)));

   ///<summary>Throws <paramref name="exceptionFactory"/>() if <paramref name="predicate"/> returns false when applied to <paramref name="it"/> otherwise returns <paramref name="it"/></summary>
   public static T _assert<T>(this T it, Predicate<T> predicate, Func<Exception> exceptionFactory)
   {
      if(!predicate(it))
         throw exceptionFactory();
      return it;
   }
}
