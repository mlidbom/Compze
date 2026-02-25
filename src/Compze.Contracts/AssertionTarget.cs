using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

/// <summary>A zero-allocation wrapper that carries the value being asserted on together with its caller-site expression. Returned by <see cref="PipeAssert._assert{T}"/> and serves as the entry point for all assertion methods.</summary>
#pragma warning disable CA1815 // Transient utility struct — equality comparison is never needed
public readonly struct AssertionTarget<T>(T value, string? valueExpression)
#pragma warning restore CA1815
{
   internal T Value { get; } = value;
   internal string? ValueExpression { get; } = valueExpression;

   [DoesNotReturn]
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   internal static void ThrowAssertionFailed(string message) => throw new AssertionFailedException(message);

   ///<summary>Throws <see cref="AssertionFailedException"/> if the value is null. Returns the value as non-nullable on success.</summary>
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   [return: NotNull]
   public T NotNull()
   {
      if(Value is null) ThrowAssertionFailed($"Assertion failed: {ValueExpression}.{nameof(PipeAssert._assert)}().{nameof(NotNull)}()");
      return Value;
   }
}

/// <summary>Extension methods on <see cref="AssertionTarget{T}"/> that require additional type constraints.</summary>
public static class AssertionTargetExtensions
{
   ///<summary>Throws <see cref="AssertionFailedException"/> if the value equals <c>default(T)</c>. Returns the value on success.</summary>
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static T NotDefault<T>(this AssertionTarget<T> target) where T : struct
   {
      if(EqualityComparer<T>.Default.Equals(target.Value, default)) AssertionTarget<T>.ThrowAssertionFailed($"Assertion failed: {target.ValueExpression}.{nameof(PipeAssert._assert)}().{nameof(NotDefault)}()");
      return target.Value;
   }
}
