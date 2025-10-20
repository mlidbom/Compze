using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Utilities.Testing.XUnit.BDD;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
class XFactDiscoverer(IMessageSink diagnosticMessageSink) : IXunitTestCaseDiscoverer
{
   readonly IMessageSink _diagnosticMessageSink = diagnosticMessageSink;

   public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
   {
      var declaringType = testMethod.Method.ToRuntimeMethod().DeclaringType;
      var currentType = testMethod.TestClass.Class.ToRuntimeType();

      if(declaringType != currentType) //We only run these tests for the classes that declares them.
         return [];

      return [new XFactTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod)];
   }
}
#pragma warning restore CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
