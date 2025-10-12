using System.Runtime.CompilerServices;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Xunit;

internal class Startup
{
   [ModuleInitializer]
   public static void Initialize()
   {
      TestFixtureHelper.SetupSerilog(null);
      TestEnv._contextProviders.Add(() =>
      {
         if(TestContext.Current.TestCase is PluggableComponentsTestCase theCase)
         {
            return theCase.Components;
         }

         return null;
      });
   }
}
