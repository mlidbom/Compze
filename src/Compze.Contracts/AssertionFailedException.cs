using System;

namespace Compze.Contracts;

/// <summary>Thrown when a <see cref="PipeAssert._assert{T}(T, Predicate{T}, string?, string?)"/> or <see cref="AssertionTarget{T}"/> assertion call fails.</summary>
public class AssertionFailedException(string message) : Exception(message);
