using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.BDD;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
class XFactDiscoverer : IXunitTestCaseDiscoverer
{
   public async ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      var declaringType = testMethod.Method.DeclaringType;
      var currentType = testMethod.TestClass.Class;

      if(declaringType != currentType) //We only run these tests for the classes that declares them.
         return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([]);

      var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, factAttribute);

      var testCase = new XFactTestCase(
         testMethod: details.ResolvedTestMethod,
         testCaseDisplayName: details.TestCaseDisplayName,
         uniqueID: details.UniqueID,
         @explicit: details.Explicit,
         skipExceptions: details.SkipExceptions,
         skipReason: details.SkipReason,
         skipType: details.SkipType,
         skipUnless: details.SkipUnless,
         skipWhen: details.SkipWhen,
         traits: testMethod.Traits.ToReadWrite(StringComparer.OrdinalIgnoreCase),
         sourceFilePath: details.SourceFilePath,
         sourceLineNumber: details.SourceLineNumber,
         timeout: details.Timeout
      );

      return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([testCase]);
   }
}
#pragma warning restore CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
