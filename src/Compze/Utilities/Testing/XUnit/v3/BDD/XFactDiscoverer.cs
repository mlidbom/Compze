using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.v3.BDD;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
class XFactDiscoverer : IXunitTestCaseDiscoverer
{
   public ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      var declaringType = testMethod.Method.DeclaringType;
      var currentType = testMethod.TestClass.Class;

      if(declaringType != currentType) //We only run these tests for the classes that declares them.
         return new(Array.Empty<IXunitTestCase>());

      var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, factAttribute);

      var testCase = new XFactTestCase(
         details.ResolvedTestMethod,
         details.TestCaseDisplayName,
         details.UniqueID,
         details.Explicit,
         testMethod.Traits.ToReadWrite(StringComparer.OrdinalIgnoreCase)
      );

      return new([testCase]);
   }
}
#pragma warning restore CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
