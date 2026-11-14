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

      if(WhyTheConfigurationCannotBeUsed((MatrixTheoryAttribute)factAttribute, testMethod) is {} whyTheConfigurationCannotBeUsed)
         return [ConfigurationError(discoveryOptions, testMethod, factAttribute, whyTheConfigurationCannotBeUsed)];

      var theoryTestCases = await base.Discover(new ReadOnlyDiscoveryOptionsWithTheoryPreEnumeration(discoveryOptions), testMethod, factAttribute).caf();
      return [..theoryTestCases.Select(ToMatrixTestCase)];
   }

   /// <summary>
   /// Why <paramref name="matrix"/> cannot run <paramref name="testMethod"/>, or <see langword="null"/> when it can.
   /// </summary>
   /// <remarks>
   /// Asked before xUnit's theory discovery reads the combinations, because that discovery hides why reading them failed: it
   /// falls back to a single test case, which <see cref="CombinationsWereNotExpandedError"/> can only report as not expanded,
   /// and reports the exception only as a diagnostic message.
   /// </remarks>
   static string? WhyTheConfigurationCannotBeUsed(MatrixTheoryAttribute matrix, IXunitTestMethod testMethod)
   {
      try
      {
         _ = matrix.CombinationRowsFor(testMethod.Method);
         return null;
      }
      catch(InvalidMatrixConfigurationException invalidConfiguration)
      {
         return invalidConfiguration.Message;
      }
#pragma warning disable CA1031 // Thrown out of Discover, an exception makes xUnit drop the rest of the test class's tests and say why only in a diagnostic message; a failing test is the one place it reaches the developer.
      catch(Exception exception)
#pragma warning restore CA1031
      {
         return $"Reading the matrix's configuration threw an exception:{Environment.NewLine}{exception}";
      }
   }

   static ExecutionErrorTestCase ConfigurationError(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute matrix,
      string whyTheConfigurationCannotBeUsed)
   {
      var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, matrix);
      return new ExecutionErrorTestCase(details.ResolvedTestMethod,
                                        details.TestCaseDisplayName,
                                        details.UniqueID,
                                        details.SourceFilePath,
                                        details.SourceLineNumber,
                                        whyTheConfigurationCannotBeUsed);
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
   /// <see cref="TheoryAttribute.DisableDiscoveryEnumeration"/>, or when it falls back for a reason it reports only as a
   /// diagnostic message. Such a test cannot run as a matrix test, so it becomes a failing test saying so, instead of an
   /// obscure crash when it runs.
   /// </summary>
   static ExecutionErrorTestCase CombinationsWereNotExpandedError(IXunitDelayEnumeratedTestCase theoryTestCase) =>
      new(theoryTestCase.TestMethod,
          theoryTestCase.TestCaseDisplayName,
          theoryTestCase.UniqueID,
          theoryTestCase.SourceFilePath,
          theoryTestCase.SourceLineNumber,
          "xUnit did not expand this matrix test into one test case per combination at discovery time, so it cannot run. "
        + $"If the matrix attribute sets {nameof(TheoryAttribute.DisableDiscoveryEnumeration)}, remove it. "
        + "Otherwise xUnit fell back for a reason it reports only as a diagnostic message: run with diagnostic messages enabled to see it.");
}
