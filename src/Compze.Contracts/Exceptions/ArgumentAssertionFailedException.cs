using System;

namespace Compze.Contracts.Exceptions;

public class ArgumentAssertionFailedException(string message) : ArgumentException(message);
