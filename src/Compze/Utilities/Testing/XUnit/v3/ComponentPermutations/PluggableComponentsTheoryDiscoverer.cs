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

      var testCases = baseCases.OfType<XunitTestCase>()
                               .Select(testCase => new PluggableComponentsTestCase(
                                          testMethod: testCase.TestMethod,
                                          testCaseDisplayName: testCase.TestCaseDisplayName,
                                          uniqueID: testCase.UniqueID,
                                          @explicit: testCase.Explicit,
                                          skipExceptions: testCase.SkipExceptions,
                                          skipReason: testCase.SkipReason,
                                          skipType: testCase.SkipType,
                                          skipUnless: testCase.SkipUnless,
                                          skipWhen: testCase.SkipWhen,
                                          traits: testCase.Traits,
                                          sourceFilePath: testCase.SourceFilePath,
                                          sourceLineNumber: testCase.SourceLineNumber,
                                          timeout: testCase.Timeout,
                                          testMethodArguments: testCase.TestMethodArguments))
                               .ToArray();

      return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(testCases);
   }
}
