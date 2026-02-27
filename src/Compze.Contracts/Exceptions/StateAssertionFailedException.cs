using System;

namespace Compze.Contracts.Exceptions;

public class StateAssertionFailedException(string message) : InvalidOperationException(message);
