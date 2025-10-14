using System;
using System.Runtime.CompilerServices;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure.XUnit.Logging;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;

namespace Compze.Tests.Infrastructure.XUnit;

public static class XUnitInfrastructureModuleInitializer
{
   [ModuleInitializer]
   public static void Initialize()
   {
      TestFixtureHelper.SetupSerilog(new XUnitTestSerilogEnricher());
      TestEnv.XunitDiscoverer = () => TestContext.Current.Value?.PluggableComponents ?? throw new Exception("No pluggable components set for current test");
   }
}
