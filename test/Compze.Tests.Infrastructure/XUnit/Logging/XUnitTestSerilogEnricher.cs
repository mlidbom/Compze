using Serilog.Core;
using Serilog.Events;
using Compze.Utilities.Testing.XUnit.ComponentCombinations;
using Xunit;

namespace Compze.Tests.Infrastructure.XUnit.Logging;

//XUnit.v3 version ready to go once v3 is stable in NCrunch is at git commit: deb6be8d66ec03db2a55f84ff28feab220ae50b1
class XUnitTestSerilogEnricher : ILogEventEnricher
{
   public void Enrich(LogEvent logTevent, ILogEventPropertyFactory propertyFactory)
   {
      var testCase = TestContext.Current.TestCase;

      var pluggableComponents = ComponentCombination.TryGetCurrent()?.TryExtractPluggableComponents();

      logTevent.AddOrUpdateProperty(
         propertyFactory.CreateProperty("XUnit",
                                        new Dictionary<string, object>
                                        {
                                           ["Container"] = pluggableComponents?.DiContainer.ToString() ?? "",
                                           ["SqlLayer"] = pluggableComponents?.SqlLayer.ToString() ?? "",
                                           ["TestClass"] = testCase?.TestClass?.TestClassName ?? "missing",
                                           ["TestName"] = testCase?.TestMethod?.MethodName ?? testCase?.TestCaseDisplayName ?? "missing",
                                        },
                                        destructureObjects: true));
   }
}
