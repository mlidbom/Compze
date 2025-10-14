using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure.XUnit.Logging;

namespace Compze.Tests.Infrastructure.XUnit;

public static class XUnitInfrastructureModuleInitializer
{
   internal static readonly AsyncLocal<Tessaging.Hosting.Testing.PluggableComponents?> CurrentPluggableComponents = new();

   [ModuleInitializer]
   public static void Initialize()
   {
      TestFixtureHelper.SetupSerilog(new XUnitTestEnricher());
      TestEnv.XunitDiscoverer = () => CurrentPluggableComponents.Value ?? throw new Exception("No pluggable components set for current test");
   }
}
