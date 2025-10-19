using System.Collections.Generic;
using System.Linq;
using Compze.Tessaging.Hosting.Testing;
using Compze.Wiring.Testing.Sql;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

//XUnit.v3 version ready to go once v3 is stable in NCrunch is at git commit: deb6be8d66ec03db2a55f84ff28feab220ae50b1
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

      var excludedSqlLayersAttribute = factAttribute.GetNamedArgument<SqlLayer[]>(nameof(PCTAttribute.ExcludeSqlLayers));
      var excludedSqlLayers = excludedSqlLayersAttribute ?? [];

      var combinations = PluggableComponentsReader.Combinations
                                                  .Where(combo => !excludedSqlLayers.Contains(combo.SqlLayer))
                                                  .ToList();

      var testCases = combinations
                     .Select(combination =>
                                new PluggableComponentsTestCase(
                                   diagnosticMessageSink: _diagnosticMessageSink,
                                   defaultMethodDisplay: discoveryOptions.MethodDisplayOrDefault(),
                                   defaultMethodDisplayOptions: discoveryOptions.MethodDisplayOptionsOrDefault(),
                                   testMethod: testMethod,
                                   combination: combination))
                     .ToArray();

      return testCases;
   }
}
