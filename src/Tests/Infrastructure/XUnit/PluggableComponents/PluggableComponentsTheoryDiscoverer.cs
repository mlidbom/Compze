using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

class PluggableComponentsTheoryDiscoverer : IXunitTestCaseDiscoverer
{
   public async ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      var pgAttribute = (PluggableComponentsTheoryAttribute)factAttribute;
      var combinations = PluggableComponentsReader.GetCombinations()
                                                  .Where(combo => !pgAttribute.ExcludeSqlLayers.Contains(combo.SqlLayer))
                                                  .ToList();

      var testCases = combinations
                     .Select(combination => new PluggableComponentsTestCase(
                                testMethod: testMethod,
                                combination: combination,
                                testCaseDisplayName: $"{testMethod.Method.Name}({combination})",
                                uniqueId: $"{testMethod.UniqueID}.{combination}",
                                @explicit: pgAttribute.Explicit,
                                timeout: pgAttribute.Timeout,
                                testMethodArguments: []))
                     .ToArray();

      return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(testCases);
   }
}
