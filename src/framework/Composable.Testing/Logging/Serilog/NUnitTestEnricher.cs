using NUnit.Framework;
using Serilog.Core;
using Serilog.Events;

namespace Composable.Testing.Logging.Serilog;

class NUnitTestEnricher : ILogEventEnricher
{
   public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
   {
      logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("NUnit_Test", TestContext.CurrentContext.Test.FullName));
      logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("NUnit_TestId", TestContext.CurrentContext.Test.ID));
   }
}
