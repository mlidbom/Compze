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
      if(testMethod.Method.DeclaringType != testMethod.TestClass.Class) //We only run these tests for the classes that declares them. Just like XFact
         return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([]);

      var attribute = ((PluggableComponentsTheoryAttribute)factAttribute);

      var testCases = PluggableComponentsReader
                     .Permutations
                     .Exclude(attribute.Exclude)
                     .Select(permutation =>
                      {
                         var testCaseDetails = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions,
                                                                                          testMethod,
                                                                                          factAttribute,
                                                                                          testMethodArguments: [permutation.ToString()]);

                         return new PluggableComponentsTestCase(
                            new TestCaseDetails(testCaseDetails),
                            traits: testMethod.Traits.ToReadWrite(StringComparer.OrdinalIgnoreCase),
                            permutation: permutation);
                      })
                     .ToArray();

      return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(testCases);
   }
}
