using System;

namespace Compze.Contracts;

public class InvariantViolatedException(string message) : Exception(message);
public class InvalidResultException(string message) : Exception(message);
