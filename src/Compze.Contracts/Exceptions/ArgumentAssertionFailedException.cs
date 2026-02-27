using System;

namespace Compze.Contracts.Exceptions;

/// <summary>Thrown when a <see cref="Contract.Argument"/> assertion fails.</summary>
public class ArgumentAssertionFailedException(string message) : ArgumentException(message);
