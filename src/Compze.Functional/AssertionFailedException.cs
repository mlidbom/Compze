using System;

namespace Compze.Functional;

/// <summary>Thrown when a <see cref="Pipe._assert{T}(T, Predicate{T}, string?)"/> call fails.</summary>
public class AssertionFailedException(string message) : Exception(message);
