using System;

namespace Compze.Contracts.Exceptions;

public class InvariantViolatedException(string message) : Exception(message);
