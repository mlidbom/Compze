using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.xUnitMatrix;

#pragma warning  disable CA1812 // Avoid uninstantiated internal classes # this is used by xUnit via reflection
class MatrixTheoryDiscoverer : TheoryDiscoverer
{
   public override async ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      if(testMethod.Method.DeclaringType != testMethod.TestClass.Class)
         return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([]).caf();

      var baseCases = await base.Discover(discoveryOptions, testMethod, factAttribute).caf();

      var testCases = baseCases.Select(testCaseInterface =>
                                {
                                   // This ensures ExecutionErrorTestCase and other special cases are preserved
                                   if(testCaseInterface is not XunitTestCase xunitTestCase)
                                      return testCaseInterface;

                                   return new MatrixCombinationTestCase(xunitTestCase);
                                })
                               .ToArray();

      return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(testCases).caf();
   }
}
