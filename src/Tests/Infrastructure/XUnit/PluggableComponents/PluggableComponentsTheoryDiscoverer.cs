using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

#pragma warning disable CA1812
class PluggableComponentsTheoryDiscoverer : IXunitTestCaseDiscoverer
{
#pragma warning restore CA1812
   readonly IMessageSink _diagnosticMessageSink;

   public PluggableComponentsTheoryDiscoverer(IMessageSink diagnosticMessageSink) => _diagnosticMessageSink = diagnosticMessageSink;

   public IEnumerable<IXunitTestCase> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      ITestMethod testMethod,
      IAttributeInfo factAttribute)
   {
      var declaringType = testMethod.Method.ToRuntimeMethod().DeclaringType;
      var currentType = testMethod.TestClass.Class.ToRuntimeType();

      if(declaringType != currentType) //We only run these tests for the classes that declares them. Just like XFact
         return [];

      var excludedSqlLayersAttribute = factAttribute.GetNamedArgument<string[]>(nameof(PCTAttribute.Exclude));
      var excludedSqlLayers = excludedSqlLayersAttribute ?? [];

      var combinations = PluggableComponentsReader.Combinations.Exclude(excludedSqlLayers);

      var testCases = combinations
                     .Select(combination =>
                                new PluggableComponentsTestCase(
                                   diagnosticMessageSink: _diagnosticMessageSink,
                                   defaultMethodDisplay: discoveryOptions.MethodDisplayOrDefault(),
                                   defaultMethodDisplayOptions: discoveryOptions.MethodDisplayOptionsOrDefault(),
                                   testMethod: testMethod,
                                   combination: string.Join(ComponentsPermutation.Separator, combination)))
                     .ToArray();

      return testCases;
   }
}
