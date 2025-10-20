using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure.XUnit.Logging;

using System;
using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit;

namespace Compze.Tests.Infrastructure.XUnit;

public static class XUnitInfrastructureModuleInitializer
{
   [ModuleInitializer]
   public static void Initialize()
   {
      TestFixtureHelper.SetupSerilog(new XUnitTestSerilogEnricher());

      TestEnv.XunitDiscoverer = () => TestContext.CurrentTestCase.ExtractPluggableComponents();
   }
}
