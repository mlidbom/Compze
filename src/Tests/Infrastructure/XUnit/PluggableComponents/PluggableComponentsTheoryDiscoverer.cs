using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Testing;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

#pragma warning disable CA1812
class PluggableComponentsTheoryDiscoverer : IXunitTestCaseDiscoverer
{
#pragma warning restore CA1812
   public async ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      var pgAttribute = (PluggableComponentsTheoryAttribute)factAttribute;
      var combinations = PluggableComponentsReader.Combinations
                                                  .Where(combo => !pgAttribute.ExcludeSqlLayers.Contains(combo.SqlLayer))
                                                  .ToList();

      var testCases = combinations
                     .Select(combination =>
                      {
                         var stableUniqueId = $"{testMethod.TestClass.Class.FullName}.{testMethod.Method.Name}.{combination}";
                         return new PluggableComponentsTestCase(
                            testMethod: testMethod,
                            combination: combination,
                            testCaseDisplayName: $"{testMethod.Method.Name}({combination})",
                            uniqueId: stableUniqueId,
                            @explicit: pgAttribute.Explicit,
                            timeout: pgAttribute.Timeout,
                            testMethodArguments: []);
                      })
                     .ToArray();

      return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(testCases);
   }
}
