using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Xunit;

[assembly: AssemblyFixture(typeof(XUnitStartupInfrastructure))]

namespace Compze.Tests.Infrastructure.XUnit;

public class XUnitStartupInfrastructure
{
   [ModuleInitializer]
   public static void Initialize()
   {
      TestFixtureHelper.SetupSerilog(null);
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
