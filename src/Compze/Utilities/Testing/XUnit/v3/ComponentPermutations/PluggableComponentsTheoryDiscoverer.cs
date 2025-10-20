using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

#pragma warning disable CA1812
class PluggableComponentsTheoryDiscoverer : IXunitTestCaseDiscoverer
{
#pragma warning restore CA1812

   public async ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      var declaringType = testMethod.Method.DeclaringType;
      var currentType = testMethod.TestClass.Class;

      if(declaringType != currentType) //We only run these tests for the classes that declares them. Just like XFact
         return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([]);

      // In v3, factAttribute is the actual attribute instance
      var pctAttribute = factAttribute as PluggableComponentsTheoryAttribute;
      var excludedSqlLayers = pctAttribute?.Exclude ?? [];

      var testCases = PluggableComponentsReader
                     .Permutations
                     .Exclude(excludedSqlLayers)
                     .Select(permutation =>
                      {
                         var testCaseDetails = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions,
                                                                                          testMethod,
                                                                                          factAttribute,
                                                                                          testMethodArguments: [permutation.ToString()]);

                         return new PluggableComponentsTestCase(
                            testMethod: testCaseDetails.ResolvedTestMethod,
                            testCaseDisplayName: testCaseDetails.TestCaseDisplayName,
                            uniqueID: testCaseDetails.UniqueID,
                            @explicit: testCaseDetails.Explicit,
                            traits: testMethod.Traits.ToReadWrite(StringComparer.OrdinalIgnoreCase),
                            permutation: permutation);
                      })
                     .ToArray();

      return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(testCases);
   }
}
