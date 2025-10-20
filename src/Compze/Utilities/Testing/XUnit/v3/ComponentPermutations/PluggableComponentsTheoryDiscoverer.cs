using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.v3.ComponentPermutations;

#pragma warning disable CA1812
class PluggableComponentsTheoryDiscoverer : IXunitTestCaseDiscoverer
{
#pragma warning restore CA1812

   public ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      var declaringType = testMethod.Method.DeclaringType;
      var currentType = testMethod.TestClass.Class;

      if(declaringType != currentType) //We only run these tests for the classes that declares them. Just like XFact
         return new(Array.Empty<IXunitTestCase>());

      // In v3, factAttribute is the actual attribute instance
      var pctAttribute = factAttribute as PluggableComponentsTheoryAttribute;
      var excludedSqlLayers = pctAttribute?.Exclude ?? [];

      var testCases = PluggableComponentsReader
            .Permutations
            .Exclude(excludedSqlLayers)
            .Select(permutation =>
            {
               var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, factAttribute);
               
               return new PluggableComponentsTestCase(
                  testMethod: details.ResolvedTestMethod,
                  testCaseDisplayName: details.TestCaseDisplayName,
                  uniqueID: details.UniqueID,
                  @explicit: details.Explicit,
                  traits: testMethod.Traits.ToReadWrite(StringComparer.OrdinalIgnoreCase),
                  permutationString: permutation.ToString());
            })
            .ToArray();

      return new(testCases);
   }
}
