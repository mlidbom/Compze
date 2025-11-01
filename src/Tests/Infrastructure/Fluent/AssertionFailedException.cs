using System;

namespace Compze.Tests.Infrastructure.Fluent;

public class AssertionFailedException(string message, Exception? inner = null) : Exception(message, inner);
