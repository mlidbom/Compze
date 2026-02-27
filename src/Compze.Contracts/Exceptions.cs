using System;

namespace Compze.Contracts;

public class InvariantViolatedException(string message) : Exception(message);

public class ArgumentAssertionFailedException(string message) : ArgumentException(message);
