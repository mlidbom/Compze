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

      // Build deterministic ID from full type name + method name + combination
      // This ensures NCrunch gets the same ID during discovery and execution phases
      var fullName = testMethod.TestClass.Class.FullName ?? testMethod.TestClass.Class.Name;
      
      // XUnit v3 requires that SkipUnless and SkipWhen are mutually exclusive
      // Only pass non-null/non-empty values, and ensure both aren't set
      var skipUnless = !string.IsNullOrEmpty(pgAttribute.SkipUnless) ? pgAttribute.SkipUnless : null;
      var skipWhen = !string.IsNullOrEmpty(pgAttribute.SkipWhen) ? pgAttribute.SkipWhen : null;
      
      // If both are somehow set, prefer SkipUnless (defensive)
      if(skipUnless != null && skipWhen != null)
         skipWhen = null;
      
      var testCases = combinations
                     .Select(combination =>
                      {
                         var stableUniqueId = $"{fullName}.{testMethod.Method.Name}.{combination}";
                         return new PluggableComponentsTestCase(
                            testMethod: testMethod,
                            combination: combination,
                            testCaseDisplayName: $"{testMethod.Method.Name}({combination})",
                            uniqueId: stableUniqueId,
                            @explicit: pgAttribute.Explicit,
                            skipReason: pgAttribute.Skip,
                            skipType: pgAttribute.SkipType,
                            skipUnless: skipUnless,
                            skipWhen: skipWhen,
                            timeout: pgAttribute.Timeout,
                            testMethodArguments: []);
                      })
                     .ToArray();

      return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(testCases);
   }
}
