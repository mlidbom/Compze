using Xunit;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

#pragma warning disable CA1812
class PluggableComponentsTheoryDiscoverer : TheoryDiscoverer
{
#pragma warning restore CA1812

   public override async ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      var declaringType = testMethod.Method.DeclaringType;
      var currentType = testMethod.TestClass.Class;

      if(declaringType != currentType) //We only run these tests for the classes that declares them. Just like XFact
         return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([]);

      var pctAttribute = (PluggableComponentsTheoryAttribute)factAttribute;

      var baseCases = await base.Discover(discoveryOptions, testMethod, factAttribute);

      var testCases = baseCases.Select(testCase =>
                      {
                         // This ensures ExecutionErrorTestCase and other special cases are preserved
                         if(testCase is not XunitTestCase xunitTestCase)
                            return testCase;

                         return new PluggableComponentsTestCase(xunitTestCase);
                      })
                     .ToArray();

      return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(testCases);
   }
}
