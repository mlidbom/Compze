using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;

namespace Compze.Tests.Infrastructure.XUnit.Logging;

//XUnit.v3 version ready to go once v3 is stable in NCrunch is at git commit: deb6be8d66ec03db2a55f84ff28feab220ae50b1
class DummyXUnitTestEnricher : ILogEventEnricher
{
   public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
   {
      // XUnit v2 doesn't have TestContext.Current like v3 does
      // We could potentially add test info here if needed, but for now just skip it
      // The combination info is available via TestEnv if needed
   }
}
