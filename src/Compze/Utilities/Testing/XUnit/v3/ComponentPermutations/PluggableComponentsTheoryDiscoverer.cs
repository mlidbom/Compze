using Xunit;
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

      var pctAttribute = (PluggableComponentsTheoryAttribute)factAttribute;

      var testCases = PluggableComponentsReader
                     .Permutations
                     .Exclude(pctAttribute.Exclude)
                     .Select(permutation =>
                      {
                         var permutationString = permutation.ToString();
                         var theoryDataRow = new TheoryDataRow(permutationString);
                         var testMethodArguments = new object?[] { permutationString };

                         var testCaseDetails = TestIntrospectionHelper.GetTestCaseDetailsForTheoryDataRow(
                            discoveryOptions,
                            testMethod,
                            pctAttribute,
                            theoryDataRow,
                            testMethodArguments);

                         return new PluggableComponentsTestCase(
                            testMethod: testCaseDetails.ResolvedTestMethod,
                            testCaseDisplayName: testCaseDetails.TestCaseDisplayName,
                            uniqueID: testCaseDetails.UniqueID,
                            @explicit: testCaseDetails.Explicit,
                            skipExceptions: testCaseDetails.SkipExceptions,
                            skipReason: testCaseDetails.SkipReason,
                            skipType: testCaseDetails.SkipType,
                            skipUnless: testCaseDetails.SkipUnless,
                            skipWhen: testCaseDetails.SkipWhen,
                            traits: TestIntrospectionHelper.GetTraits(testMethod, theoryDataRow),
                            sourceFilePath: testCaseDetails.SourceFilePath,
                            sourceLineNumber: testCaseDetails.SourceLineNumber,
                            timeout: testCaseDetails.Timeout,
                            permutation: permutation);
                      })
                     .ToArray();

      return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(testCases);
   }
}
