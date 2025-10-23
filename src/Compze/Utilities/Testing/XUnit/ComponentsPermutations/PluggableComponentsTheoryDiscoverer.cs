using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentsPermutations;

class ComponentsPermutationsTheoryDiscoverer : TheoryDiscoverer
{

   public override async ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      if(testMethod.Method.DeclaringType != testMethod.TestClass.Class)
         return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([]);

      var baseCases = await base.Discover(discoveryOptions, testMethod, factAttribute);


      var pctAttribute = (ComponentsPermutationsTheoryAttribute)factAttribute;

      var testCases = baseCases.Select(testCaseInterface =>
                                {
                                   // This ensures ExecutionErrorTestCase and other special cases are preserved
                                   if(testCaseInterface is not XunitTestCase xunitTestCase)
                                      return testCaseInterface;

                                   return new ComponentsPermutationTestCase(
                                      testCase: xunitTestCase,
                                      useTestMethodArguments: pctAttribute.UseTestMethodArgument,
                                      traits: testMethod.Traits.ToReadWrite(StringComparer.OrdinalIgnoreCase)
                                   );
                                })
                               .ToArray();

      return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(testCases);
   }
}
