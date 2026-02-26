using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure.XUnit.Logging;
using System;
using System.Runtime.CompilerServices;
using Compze.Utilities.Logging;
using Compze.Utilities.Testing.XUnit.ComponentCombinations;

namespace Compze.Tests.Infrastructure.XUnit;

public static class XUnitInfrastructureModuleInitializer
{
   [ModuleInitializer]
   public static void Initialize()
   {
      if(Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
      {
         CompzeLogger.LogLevel = LogLevel.Debug;
         CompzeLogger.LoggerFactoryMethod = XUnitTestOutputLogger.Create;
      }
      else
      {
         TestFixtureHelper.SetupSerilog(new XUnitTestSerilogEnricher());
      }

      TestEnv.XunitDiscoverer = () => ComponentCombination.Current.ToPluggableComponents();
   }
}
