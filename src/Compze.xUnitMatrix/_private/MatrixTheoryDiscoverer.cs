using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.xUnitMatrix._private;

#pragma warning  disable CA1812 // Avoid uninstantiated internal classes # this is used by xUnit via reflection
class MatrixTheoryDiscoverer : TheoryDiscoverer
{
   public override async ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      if(testMethod.Method.DeclaringType != testMethod.TestClass.Class)
         return [];

      var theoryTestCases = await base.Discover(new ReadOnlyDiscoveryOptionsWithTheoryPreEnumeration(discoveryOptions), testMethod, factAttribute).caf();
      return [..theoryTestCases.Select(ToMatrixTestCase)];
   }

   static IXunitTestCase ToMatrixTestCase(IXunitTestCase theoryTestCase) => theoryTestCase switch
   {
      IXunitDelayEnumeratedTestCase notExpandedIntoCombinations => CombinationsWereNotExpandedError(notExpandedIntoCombinations),
      XunitTestCase oneCombination => new MatrixCombinationTestCase(oneCombination),
      _ => theoryTestCase // ExecutionErrorTestCase and other special cases already carry their own outcome
   };

   /// <summary>
   /// xUnit can still decline to expand a theory into one test case per combination even though
   /// <see cref="ReadOnlyDiscoveryOptionsWithTheoryPreEnumeration"/> asks for it: when the attribute sets
   /// <see cref="TheoryAttribute.DisableDiscoveryEnumeration"/>, or when enumerating the combinations threw (xUnit then
   /// falls back silently, reporting why only as a diagnostic message). Such a test cannot run as a matrix test,
   /// so it becomes a failing test saying so, instead of an obscure crash when it runs.
   /// </summary>
   static ExecutionErrorTestCase CombinationsWereNotExpandedError(IXunitDelayEnumeratedTestCase theoryTestCase) =>
      new(theoryTestCase.TestMethod,
          theoryTestCase.TestCaseDisplayName,
          theoryTestCase.UniqueID,
          theoryTestCase.SourceFilePath,
          theoryTestCase.SourceLineNumber,
          "xUnit did not expand this matrix test into one test case per combination at discovery time, so it cannot run. "
        + $"If the matrix attribute sets {nameof(TheoryAttribute.DisableDiscoveryEnumeration)}, remove it. "
        + "Otherwise enumerating the combinations failed: run with diagnostic messages enabled to see why.");
}
