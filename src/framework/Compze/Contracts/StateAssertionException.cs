using System;

namespace Compze.Contracts;

class StateAssertionException(string message) : InvalidOperationException(message);
