using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Contracts;

/// <summary>Pipeline-friendly assertion extension methods. Uses the same _camelCase naming convention as Compze.Functional's Pipe operators so they blend naturally in pipelines./// </summary>
public static class PipeAssert
{
   [DoesNotReturn]
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   static void ThrowAssertionFailed(string message) => throw new AssertionFailedException(message);

   [DoesNotReturn]
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   static void ThrowCustomException(Func<Exception> exceptionFactory) => throw exceptionFactory();

   extension<T>(T @this)
   {
      ///<summary>Throws <see cref="AssertionFailedException"/> if <paramref name="predicate"/> returns false when applied to <paramref name="this"/>. The exception message includes the predicate source expression and the value.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public T _assert(Predicate<T> predicate, [CallerArgumentExpression(nameof(predicate))] string? predicateExpression = null)
      {
         if(!predicate(@this)) ThrowAssertionFailed($"Assertion failed: {predicateExpression} (value: {@this})");
         return @this;
      }

      ///<summary>Throws <see cref="AssertionFailedException"/> with message from <paramref name="messageFactory"/> if <paramref name="predicate"/> returns false when applied to <paramref name="this"/>. The factory is only invoked on failure, avoiding allocation in the success path.</summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public T _assert(Predicate<T> predicate, Func<T, string> messageFactory)
      {
         if(!predicate(@this)) ThrowAssertionFailed(messageFactory(@this));
         return @this;
      }

      ///<summary>Throws <paramref name="exceptionFactory"/>() if <paramref name="predicate"/> returns false when applied to <paramref name="this"/> otherwise returns <paramref name="this"/></summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public T _assert(Predicate<T> predicate, Func<Exception> exceptionFactory)
      {
         if(!predicate(@this)) ThrowCustomException(exceptionFactory);
         return @this;
      }
   }
}
