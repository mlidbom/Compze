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
      if(TestContext.Current == null) return;
      var context = TestContext.Current.Value;

      var testClassName = context.TestMethod.TestClass.Class.ToRuntimeType().GetFullNameCompilable();
      var testName = context.TestMethod.Method.Name;
      var fullTestName = $"{testClassName}.{testName}";
      logEvent.AddOrUpdateProperty(
         propertyFactory.CreateProperty("XUnit",
                                        new Dictionary<string, object>
                                        {
                                           ["Test"] = fullTestName,
                                        },
                                        destructureObjects: true));
   }
}
