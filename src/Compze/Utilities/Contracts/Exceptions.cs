using System;

namespace Compze.Utilities.Contracts;

class InvariantViolatedException(string tessage) : Exception(tessage);
class InvalidResultException(string tessage) : Exception(tessage);
