using System;
using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.Fluent;

public class AssertionFailedException(string tessage) :
   Exception($"""

              
              {tessage.Indent()}

              """);
