using System;
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
      // In XUnit v2, we need to convert ITypeInfo and IMethodInfo to runtime types to compare them properly
      // This is the key difference from just comparing Name properties
      var declaringType = testMethod.Method.ToRuntimeMethod()?.DeclaringType;
      var currentType = testMethod.TestClass.Class.ToRuntimeType();

      // Ensure we have valid types before comparing
      if(declaringType == null || currentType == null)
         return Array.Empty<IXunitTestCase>();

      // Only create a test case if the method is declared in the current class (not inherited from base)
      if(declaringType != currentType)
         return Array.Empty<IXunitTestCase>();

      return new[] { new XunitTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod) };
   }
}
