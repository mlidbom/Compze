using Serilog.Core;
using Serilog.Events;

namespace Compze.Tests.Infrastructure.XUnit.Logging;

class XUnitTestEnricher : ILogEventEnricher
{
   public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
   {
      // XUnit v2 doesn't have TestContext.Current like v3 does
      // We could potentially add test info here if needed, but for now just skip it
      // The combination info is available via TestEnv if needed
   }
}
