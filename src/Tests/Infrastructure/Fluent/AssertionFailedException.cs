using System;
using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.Fluent;

public class AssertionFailedException(string message, Exception inner = null) :
   Exception($"""
              
              {message}

              """, inner);
