using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure.XUnit.Logging;
using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentCombinations;

namespace Compze.Tests.Infrastructure.XUnit;

public static class XUnitInfrastructureModuleInitializer
{
   [ModuleInitializer]
   public static void Initialize()
   {
      TestFixtureHelper.SetupSerilog(new XUnitTestSerilogEnricher());

      TestEnv.XunitDiscoverer = () => ComponentCombination.Current.ToPluggableComponents();
   }
}
