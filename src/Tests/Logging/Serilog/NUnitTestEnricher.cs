using System.Collections.Generic;
using NUnit.Framework;
using Serilog.Core;
using Serilog.Events;

namespace Compze.Testing.Logging.Serilog;

class NUnitTestEnricher : ILogEventEnricher
{
   public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
   {
      logEvent.AddOrUpdateProperty(
         propertyFactory.CreateProperty("NUnit",
                                        new Dictionary<string, object>
                                        {
                                           ["Test"] = TestContext.CurrentContext.Test.FullName,
                                           ["Id"] = TestContext.CurrentContext.Test.ID
                                        },
                                        destructureObjects: true));
   }
}
