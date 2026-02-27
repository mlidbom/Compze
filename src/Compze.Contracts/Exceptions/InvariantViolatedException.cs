using System;

namespace Compze.Contracts.Exceptions;

/// <summary>Thrown when a <see cref="Contract.Invariant"/> assertion fails.</summary>
public class InvariantAssertionFailedException(string message) : Exception(message);
