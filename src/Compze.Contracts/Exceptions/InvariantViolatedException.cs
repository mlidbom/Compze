using System;

namespace Compze.Contracts.Exceptions;

public class InvariantAssertionFailedException(string message) : Exception(message);
