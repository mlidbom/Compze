using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Compze.Contracts.Exceptions;

// ReSharper disable ConvertToExtensionBlock

// ReSharper disable InconsistentNaming

namespace Compze.Contracts;

/// <summary>Pipeline-friendly assertion extension methods. Uses the same _camelCase naming convention as Compze.Functional's Pipe operators so they blend naturally in pipelines.</summary>
public static class PipeAssert
{
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   [DoesNotReturn] static void ThrowAssertionFailed(string message) => throw new AssertionFailedException(message);

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   [DoesNotReturn] static void ThrowCustomException(Func<Exception> exceptionFactory) => throw exceptionFactory();

   ///<summary>Returns an <see cref="PipeAssertTarget{T}"/> wrapping <paramref name="this"/> for further assertion calls such as <c>.NotNull()</c> and <c>.NotDefault()</c>.</summary>
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static PipeAssertTarget<T> _assert<T>(this T @this, [CallerArgumentExpression(nameof(@this))] string? thisExpression = null)
      => new(@this, thisExpression);

   ///<summary>Throws <see cref="AssertionFailedException"/> if <paramref name="predicate"/> returns false when applied to <paramref name="this"/>. The exception message includes the predicate source expression and the value.</summary>
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static T _assert<T>(this T @this,
                              Predicate<T> predicate,
                              [CallerArgumentExpression(nameof(@this))] string? thisExpression = null,
                              [CallerArgumentExpression(nameof(predicate))] string? predicateExpression = null)
   {
      if(!predicate(@this)) ThrowAssertionFailed($"Assertion failed: {thisExpression}.{nameof(_assert)}({predicateExpression}) ## the value of {thisExpression} was: {@this}");
      return @this;
   }

   ///<summary>Throws <see cref="AssertionFailedException"/> with message from <paramref name="messageFactory"/> if <paramref name="predicate"/> returns false when applied to <paramref name="this"/>. The factory is only invoked on failure, avoiding allocation in the success path.</summary>
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static T _assert<T>(this T @this, Predicate<T> predicate, Func<T, string> messageFactory)
   {
      if(!predicate(@this)) ThrowAssertionFailed(messageFactory(@this));
      return @this;
   }

   ///<summary>Throws <paramref name="exceptionFactory"/>() if <paramref name="predicate"/> returns false when applied to <paramref name="this"/> otherwise returns <paramref name="this"/></summary>
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static T _assert<T>(this T @this, Predicate<T> predicate, Func<Exception> exceptionFactory)
   {
      if(!predicate(@this)) ThrowCustomException(exceptionFactory);
      return @this;
   }
}
