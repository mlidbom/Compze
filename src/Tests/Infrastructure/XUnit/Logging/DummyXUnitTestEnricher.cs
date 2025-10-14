using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;

namespace Compze.Tests.Infrastructure.XUnit.Logging;

//XUnit.v3 version ready to go once v3 is stable in NCrunch is at git commit: deb6be8d66ec03db2a55f84ff28feab220ae50b1
class XUnitTestSerilogEnricher : ILogEventEnricher
{
   public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
   {
      logEvent.AddOrUpdateProperty(
         propertyFactory.CreateProperty("XUnit",
                                        new Dictionary<string, object>
                                        {
                                           ["Test"] = TestContext.Current.Value?.TestMethod.Method.Name ?? "Missing",
                                        },
                                        destructureObjects: true));
   }
}
