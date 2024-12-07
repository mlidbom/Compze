using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ScratchPad.NestedTests;

[XunitTestCaseDiscoverer("ScratchPad.NestedTests.XFactDiscoverer", "ScratchPad.Internals")]
public class XFactAttribute : FactAttribute {}

public class XFactDiscoverer(IMessageSink diagnosticMessageSink) : IXunitTestCaseDiscoverer
{
   readonly IMessageSink _diagnosticMessageSink = diagnosticMessageSink;

   public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
   {
      var declaringType = testMethod.Method.ToRuntimeMethod().DeclaringType;
      var currentType = testMethod.TestClass.Class.ToRuntimeType();

      if(declaringType != currentType) // Skip tests declared in base classes
         return Array.Empty<IXunitTestCase>();

      return [new XunitTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod)];
   }
}

// Example usage
public class Outer_scenario_duplicates
{
   readonly Guid _guid = Guid.NewGuid();

   [XFact] public void Outer_test_1() => Guid.NewGuid().Should().NotBeEmpty();

   public class Inner_scenario : Outer_scenario_duplicates
   {
      [XFact] public void Inner_fact() => Guid.NewGuid().Should().NotBeEmpty();
   }
}
