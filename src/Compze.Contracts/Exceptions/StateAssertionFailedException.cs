namespace Compze.Contracts.Exceptions;

/// <summary>Thrown when a <see cref="Contract.State"/> assertion fails.</summary>
public class StateAssertionFailedException(string message) : InvalidOperationException(message);
