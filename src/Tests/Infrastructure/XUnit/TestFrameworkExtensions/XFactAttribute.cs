using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;

/// <summary>
/// This attribute will run the test exclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
/// This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
[XunitTestCaseDiscoverer("Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions.XFactAttributeTestCaseDiscoverer", "Compze.Tests.Infrastructure.XUnit")]
public sealed class XFactAttribute : FactAttribute {}

public class XFactAttributeTestCaseDiscoverer : IXunitTestCaseDiscoverer
{
   readonly IMessageSink _diagnosticMessageSink;

   public XFactAttributeTestCaseDiscoverer(IMessageSink diagnosticMessageSink)
   {
      _diagnosticMessageSink = diagnosticMessageSink;
   }

   public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
   {
      var declaringType = testMethod.Method.Type;
      var currentType = testMethod.TestClass.Class;

      // Ensure we have valid types before comparing
      if(declaringType == null || currentType == null)
         return [];

      if(declaringType.Name != currentType.Name)
         return [];

      return [new XunitTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod)];
   }
}
