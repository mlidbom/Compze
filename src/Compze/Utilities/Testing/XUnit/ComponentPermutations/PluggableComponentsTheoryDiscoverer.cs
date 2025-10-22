using Xunit;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class PluggableComponentsTheoryDiscoverer : TheoryDiscoverer
{

   public override async ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      if(testMethod.Method.DeclaringType != testMethod.TestClass.Class)
         return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([]);

      var baseCases = await base.Discover(discoveryOptions, testMethod, factAttribute);


      var pctAttribute = (PluggableComponentsTheoryAttribute)factAttribute;
      var componentEnumTypes = pctAttribute.ComponentEnumTypes;

      var testCases = baseCases.Select(testCaseInterface =>
                                {
                                   // This ensures ExecutionErrorTestCase and other special cases are preserved
                                   if(testCaseInterface is not XunitTestCase xunitTestCase)
                                      return testCaseInterface;

                                   return new PluggableComponentsTestCase(
                                      xunitTestCase,
                                      traits: testMethod.Traits.ToReadWrite(StringComparer.OrdinalIgnoreCase),
                                      componentEnumTypes: componentEnumTypes
                                   );
                                })
                               .ToArray();

      return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(testCases);
   }
}
