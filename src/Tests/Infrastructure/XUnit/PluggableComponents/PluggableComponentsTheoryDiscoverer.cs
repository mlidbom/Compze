using System.Collections.Generic;
using System.Linq;
using Compze.Tessaging.Hosting.Testing;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

#pragma warning disable CA1812
class PluggableComponentsTheoryDiscoverer : IXunitTestCaseDiscoverer
{
#pragma warning restore CA1812
   readonly IMessageSink _diagnosticMessageSink;

   public PluggableComponentsTheoryDiscoverer(IMessageSink diagnosticMessageSink)
   {
      _diagnosticMessageSink = diagnosticMessageSink;
   }

   public IEnumerable<IXunitTestCase> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      ITestMethod testMethod,
      IAttributeInfo factAttribute)
   {
      var excludedSqlLayersAttribute = factAttribute.GetNamedArgument<Wiring.SqlLayer[]>(nameof(PluggableComponentsTheoryAttribute.ExcludeSqlLayers));
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
