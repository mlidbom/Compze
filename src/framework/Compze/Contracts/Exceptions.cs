using System;

namespace Compze.Contracts;

class InvariantViolatedException(string message) : Exception(message);
class InvalidResultException(string message) : Exception(message);
