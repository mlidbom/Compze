using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.BDD;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
public class ExclusiveFactDiscoverer : IXunitTestCaseDiscoverer
{
   public async ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      var declaringType = testMethod.Method.DeclaringType;
      var currentType = testMethod.TestClass.Class;

      if(declaringType != currentType) //We only run these tests for the classes that declares them.
         return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([]).caf();

      var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, factAttribute);

      #pragma warning disable CA2000// We are passing this disposable into a constructor of an object we don't own
      var testCase = new ExclusiveFactTestCase(
         details: new TestCaseDetails(details),
         traits: testMethod.Traits.ToReadWrite(StringComparer.OrdinalIgnoreCase)
      );

      return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([testCase]).caf();
   }
}
#pragma warning restore CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
