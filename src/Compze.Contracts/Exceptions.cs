using System;

namespace Compze.Contracts;

public class InvariantViolatedException(string message) : Exception(message);
