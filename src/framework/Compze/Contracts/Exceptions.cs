using System;

namespace Compze.Contracts;

class ArgumentAssertionException(string message) : ArgumentException(message);
class InvariantAssertionException(string message) : Exception(message);
class ResultAssertionException(string message) : Exception(message);
class StateAssertionException(string message) : InvalidOperationException(message);
