using Compze.Tests.Infrastructure;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Compze.Wiring;

namespace Compze.Tessaging.Hosting.Testing;

///<summary>TestEnvironment class. Shortened name since it is referenced statically and has nested types</summary>
public static partial class TestEnv
{
   internal static IList<Func<PluggableComponents?>> _contextProviders => new List<Func<PluggableComponents?>>();
}
