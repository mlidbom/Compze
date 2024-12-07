using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace ScratchPad.NestedTests;

// Register the discoverer using typeof
[XunitTestCaseDiscoverer(typeof(XFactDiscoverer))]
public sealed class XFactAttribute : FactAttribute {}

// Custom Test Case Discoverer
public class XFactDiscoverer : IXunitTestCaseDiscoverer
{
   public ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, IXunitTestMethod testMethod, IFactAttribute factAttribute)
   {
      var declaringType = testMethod.Method.DeclaringType;
      var currentType = testMethod.TestClass.Class;

      if(declaringType != currentType)
         return ValueTask.FromResult((IReadOnlyCollection<IXunitTestCase>)Array.Empty<IXunitTestCase>());

      return ValueTask.FromResult((IReadOnlyCollection<IXunitTestCase>)
                                  [
                                     new XunitTestCase(
                                        testMethod: testMethod,
                                        testCaseDisplayName:"Nonsense",
                                        uniqueID: testMethod.UniqueID,
                                        @explicit: factAttribute.Explicit)
                                  ]);
   }
}

// Example usage
public class Outer_scenario_duplicates
{
   readonly Guid _guid = Guid.NewGuid();

   [XFact] public void Outer_test_1() => _guid.Should().NotBeEmpty();

   public class Inner_scenario : Outer_scenario_duplicates
   {
      [XFact] public void Inner_fact() => _guid.Should().NotBeEmpty();
   }
}
