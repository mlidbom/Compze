using Compze.Tessaging.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Tests.Infrastructure.XUnit.Logging;
using System.Runtime.CompilerServices;
using Compze.xUnitMatrix;

namespace Compze.Tests.Infrastructure.XUnit;

public static class XUnitInfrastructureModuleInitializer
{
   [ModuleInitializer]
   public static void Initialize()
   {
      TestFixtureHelper.SetupSerilog(new XUnitTestSerilogEnricher(), new XUnitTestOutputHelperSink());

      TestEnv.XunitDiscoverer = () => ComponentCombination.Current.ToPluggableComponents();
   }
}
