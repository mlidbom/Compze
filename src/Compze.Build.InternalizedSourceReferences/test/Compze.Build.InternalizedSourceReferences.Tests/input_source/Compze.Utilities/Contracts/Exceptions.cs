using System;

namespace Compze.Utilities.Contracts;

public class InvariantViolatedException(string message) : Exception(message);
public class InvalidResultException(string message) : Exception(message);
