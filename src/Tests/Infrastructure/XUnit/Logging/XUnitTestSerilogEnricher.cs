using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.Testing.XUnit;

namespace Compze.Tests.Infrastructure.XUnit.Logging;

//XUnit.v3 version ready to go once v3 is stable in NCrunch is at git commit: deb6be8d66ec03db2a55f84ff28feab220ae50b1
class XUnitTestSerilogEnricher : ILogEventEnricher
{
   public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
   {
      if(TestContext.CurrentTestCase == null) return;
      var testCase = TestContext.CurrentTestCase;

      var pluggableComponents = ComponentContext.CurrentPermutation?.TryExtractPluggableComponents();

      logEvent.AddOrUpdateProperty(
         propertyFactory.CreateProperty("XUnit",
                                        new Dictionary<string, object>
                                        {
                                           ["Container"] = pluggableComponents?.DiContainer.ToString() ?? "",
                                           ["SqlLayer"] = pluggableComponents?.SqlLayer.ToString() ?? "",
                                           ["TestClass"] = testCase.TestMethod.TestClass.Class.ToRuntimeType().GetFullNameCompilable(),
                                           ["TestName"] = testCase.DisplayName,
                                        },
                                        destructureObjects: true));
   }
}
