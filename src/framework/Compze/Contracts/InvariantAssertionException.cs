using System;

namespace Compze.Contracts;

class InvariantAssertionException(string message) : Exception(message);
