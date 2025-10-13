using System;
using System.Runtime.CompilerServices;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure.XUnit.Logging;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Xunit;

namespace Compze.Tests.Infrastructure.XUnit;

public static class XUnitInfrastructureModuleInitializer
{
   [ModuleInitializer]
   public static void Initialize()
   {
      TestFixtureHelper.SetupSerilog(new XUnitTestEnricher());
      TestEnv.XunitDiscoverer = () =>
      {
         if(TestContext.Current.TestCase is PluggableComponentsTestCase theCase)
         {
            return theCase.Components;
         }

         throw new Exception($"Current test is not a {typeof(PluggableComponentsTestCase)}");
      };
   }
}
