using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Tests.Infrastructure.XUnit.Logging;

//XUnit.v3 version ready to go once v3 is stable in NCrunch is at git commit: deb6be8d66ec03db2a55f84ff28feab220ae50b1
class XUnitTestSerilogEnricher : ILogEventEnricher
{
   public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
   {
      if(TestContext.CurrentTestCase == null) return;
      var testCase = TestContext.CurrentTestCase;

      logEvent.AddOrUpdateProperty(
         propertyFactory.CreateProperty("XUnit",
                                        new Dictionary<string, object>
                                        {
                                           ["TestClass"] = testCase.TestMethod.TestClass.Class.ToRuntimeType().GetFullNameCompilable(),
                                           ["TestName"] = testCase.DisplayName,
                                        },
                                        destructureObjects: true));
   }
}
