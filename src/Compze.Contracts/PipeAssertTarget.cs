using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Compze.Contracts.Exceptions;

// ReSharper disable MemberCanBeInternal

namespace Compze.Contracts;

/// <summary>A zero-allocation wrapper that carries the value being asserted on together with its caller-site expression. Returned by <see cref="PipeAssert._assert{T}(T, string?)"/> and serves as the entry point for all assertion methods.</summary>
#pragma warning disable CA1815 // Transient utility struct — equality comparison is never needed
public readonly struct PipeAssertTarget<T>(T value, string? valueExpression)
#pragma warning restore CA1815
{
   /// <summary>The value being asserted on.</summary>
   [EditorBrowsable(EditorBrowsableState.Never)]
   public T Value { get; } = value;

   /// <summary>The caller-site expression that produced <see cref="Value"/>.</summary>
   [EditorBrowsable(EditorBrowsableState.Never)] string? ValueExpression { get; } = valueExpression;

   /// <summary>Throws <see cref="AssertionFailedException"/> with the specified message. Use this from custom assertion extension methods.</summary>
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   [EditorBrowsable(EditorBrowsableState.Never)]
   [DoesNotReturn] public void ThrowAssertionFailed([CallerMemberName] string? callerName = null) => throw new AssertionFailedException($"{ValueExpression}.{nameof(PipeAssert._assert)}().{callerName}()");
}
