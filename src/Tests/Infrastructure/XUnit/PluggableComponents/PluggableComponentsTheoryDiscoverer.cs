using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Testing;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

class PluggableComponentsTheoryDiscoverer : IXunitTestCaseDiscoverer
{
#pragma warning restore CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
   public async ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      var combinations = PluggableComponentsReader.GetCombinations();

      // Filter out excluded SQL layers if specified
      if(factAttribute is PluggableComponentsTheoryAttribute { ExcludeSqlLayers.Length: > 0 } theoryAttribute)
      {
         var excludedLayers = theoryAttribute.ExcludeSqlLayers;
         combinations = combinations
                       .Where(combo =>
                        {
                           var context = new PluggableComponentTestContext(combo);
                           return !excludedLayers.Contains(TestEnv.SqlLayer);
                        })
                       .ToList();
      }

      var testCases = combinations
                     .Select(combination =>
                      {
                         // Create and pass a PluggableComponentTestContext instance
                         var arguments = new object[] { new PluggableComponentTestContext(combination) };

                         return new PluggableComponentsTestCase(
                            testMethod: testMethod,
                            combination: combination,
                            testCaseDisplayName: $"{testMethod.Method.Name}({combination})",
                            uniqueId: $"{testMethod.UniqueID}.{combination}",
                            @explicit: factAttribute.Explicit,
                            timeout: factAttribute.Timeout,
                            testMethodArguments: arguments);
                      })
                     .ToArray();

      return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(testCases);
   }
}
