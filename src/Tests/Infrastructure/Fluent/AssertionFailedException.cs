using System;
using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.Fluent;

public class AssertionFailedException(string message) :
   Exception($"""

              
              {message.Indent()}

              """);
