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

   ///<summary>Throws <see cref="AssertionFailedException"/> if <paramref name="this"/> is null. Returns the value as non-nullable on success.</summary>
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static T _assertNotNull<T>([NotNull] this T? @this, [CallerArgumentExpression(nameof(@this))] string? thisExpression = null) where T : class
   {
      if(@this is null) ThrowAssertionFailed($"Assertion failed: {thisExpression}.{nameof(_assertNotNull)}()");
      return @this;
   }

   ///<summary>Throws <see cref="AssertionFailedException"/> if <paramref name="this"/> is null. Returns the value as non-nullable on success.</summary>
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static T _assertNotNull<T>([NotNull] this T? @this, [CallerArgumentExpression(nameof(@this))] string? thisExpression = null) where T : struct
   {
      if(@this is null) ThrowAssertionFailed($"Assertion failed: {thisExpression}.{nameof(_assertNotNull)}()");
      return @this.Value;
   }

   ///<summary>Throws <see cref="AssertionFailedException"/> if <paramref name="this"/> equals <c>default(T)</c>. Returns <paramref name="this"/> on success.</summary>
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static T _assertNotDefault<T>(this T @this, [CallerArgumentExpression(nameof(@this))] string? thisExpression = null) where T : struct, IEquatable<T>
   {
      if(@this.Equals(default)) ThrowAssertionFailed($"Assertion failed: {thisExpression}.{nameof(_assertNotDefault)}()");
      return @this;
   }

   ///<summary>Throws <see cref="AssertionFailedException"/> if <paramref name="this"/> is null or its value equals <c>default(T)</c>. Returns the non-nullable, non-default value on success.</summary>
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static T _assertNotNullOrDefault<T>([NotNull] this T? @this, [CallerArgumentExpression(nameof(@this))] string? thisExpression = null) where T : struct, IEquatable<T>
   {
      if(@this is null) ThrowAssertionFailed($"Assertion failed: {thisExpression}.{nameof(_assertNotNullOrDefault)}() ## {thisExpression} was null");
      if(@this.Value.Equals(default)) ThrowAssertionFailed($"Assertion failed: {thisExpression}.{nameof(_assertNotNullOrDefault)}() ## {thisExpression} was: {@this.Value}");
      return @this.Value;
   }
}
