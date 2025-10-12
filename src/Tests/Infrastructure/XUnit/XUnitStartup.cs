using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Xunit;

[assembly: AssemblyFixture(typeof(XUnitStartupInfrastructure))]

namespace Compze.Tests.Infrastructure.XUnit;

public class XUnitStartupInfrastructure : IAsyncLifetime
{
   public XUnitStartupInfrastructure()
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
