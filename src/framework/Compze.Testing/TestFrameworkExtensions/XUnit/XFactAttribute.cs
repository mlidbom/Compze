using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Testing.TestFrameworkExtensions.XUnit;

///<summary>
/// This attribute will run the test eXclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
///This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
[XunitTestCaseDiscoverer(typeof(XFactAttributeTestCaseDiscoverer))]
sealed class XFactAttribute : FactAttribute {}

class XFactAttributeTestCaseDiscoverer : IXunitTestCaseDiscoverer
{
   public ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, IXunitTestMethod testMethod, IFactAttribute factAttribute)
   {
      var declaringType = testMethod.Method.DeclaringType;
      var currentType = testMethod.TestClass.Class;

      if(declaringType != currentType)
         return ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(Array.Empty<IXunitTestCase>());

      return ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([
                                                                          new XunitTestCase(
                                                                             testMethod: testMethod,
                                                                             testCaseDisplayName: testMethod.Method.Name,
                                                                             uniqueID: $"{testMethod.UniqueID}.{testMethod.Method.Name}",
                                                                             @explicit: factAttribute.Explicit)
                                                                       ]);
   }
}
