using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure.XUnit.Logging;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using System;
using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure.XUnit;

public static class XUnitInfrastructureModuleInitializer
{
   [ModuleInitializer]
   public static void Initialize()
   {
      TestFixtureHelper.SetupSerilog(new XUnitTestSerilogEnricher());

      TestEnv.XunitDiscoverer = () =>
      {
         if(TestContext.CurrentTestCase == null) throw new Exception("No test context has been set");
         if(typeof(PluggableComponentsTestCase) != TestContext.CurrentTestCase.GetType()) throw new Exception("The current test is not a pluggable components tes");
         return ((PluggableComponentsTestCase)TestContext.CurrentTestCase!).Components;
      };
   }
}
