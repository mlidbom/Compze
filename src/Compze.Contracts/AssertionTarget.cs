using System;
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
}
