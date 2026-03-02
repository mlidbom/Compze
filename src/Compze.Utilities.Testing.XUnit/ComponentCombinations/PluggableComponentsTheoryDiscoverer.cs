using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentCombinations;

#pragma warning  disable CA1812 // Avoid uninstantiated internal classes # this is used by xUnit via reflection
class ComponentCombinationsTheoryDiscoverer : TheoryDiscoverer
{
   public override async ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      if(testMethod.Method.DeclaringType != testMethod.TestClass.Class)
         return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([]).caf();

      var baseCases = await base.Discover(discoveryOptions, testMethod, factAttribute).caf();

      var pctAttribute = (ComponentCombinationsTheoryAttribute)factAttribute;

      var testCases = baseCases.Select(testCaseInterface =>
                                {
                                   // This ensures ExecutionErrorTestCase and other special cases are preserved
                                   if(testCaseInterface is not XunitTestCase xunitTestCase)
                                      return testCaseInterface;

                                   return new ComponentCombinationTestCase(
                                      testCase: xunitTestCase,
                                      useTestMethodArguments: pctAttribute.UseTestMethodArgument
                                   );
                                })
                               .ToArray();

      return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(testCases).caf();
   }
}
