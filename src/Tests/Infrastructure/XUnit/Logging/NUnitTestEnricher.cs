using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Compze.Tests.Infrastructure.XUnit.Logging;

class XUnitTestEnricher : ILogEventEnricher
{
   public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
   {
      logEvent.AddOrUpdateProperty(
         propertyFactory.CreateProperty("NUnit",
                                        new Dictionary<string, object>
                                        {
                                           ["Test"] = TestContext.Current.Test!.TestDisplayName,
                                           ["Id"] = TestContext.Current.Test!.UniqueID
                                        },
                                        destructureObjects: true));
   }
}