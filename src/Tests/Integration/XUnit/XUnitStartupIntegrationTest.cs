using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Compze.Tests.Integration.XUnit;
using Xunit;

[assembly: AssemblyFixture(typeof(XUnitStartupIntegrationTest))]

namespace Compze.Tests.Integration.XUnit;

public class XUnitStartupIntegrationTest : IAsyncLifetime
{
   public XUnitStartupIntegrationTest()
   {
      TestFixtureHelper.SetupSerilog(null);
      TestEnv.XunitDiscoverer = () =>
      {
         if(TestContext.Current.TestCase is PluggableComponentsTestCase theCase)
         {
            return theCase.Components;
         }

         return null;
      };
   }

   public async ValueTask DisposeAsync() => await ValueTask.CompletedTask;

   public async ValueTask InitializeAsync() => await ValueTask.CompletedTask;
}
