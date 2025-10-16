using System.Globalization;
using Compze.Utilities.Logging;
using Compze.Utilities.Logging.Serilog;
using Serilog;
using Serilog.Core;
using Serilog.Exceptions;

namespace Compze.Tests.Infrastructure;

/// <summary>
/// Common test fixture setup/teardown logic
/// </summary>
public static class TestFixtureHelper
{
   public static void PerformTeardown()
   {
      try
      {
         Log.CloseAndFlushAsync().AsTask().GetAwaiter().GetResult();
      }
      catch
      {
         // ignore: really nothing we can do here;
      }
   }

   public static void SetupSerilog(ILogEventEnricher? testEnricher)
   {
      var config = new LoggerConfiguration()
                  .Enrich.WithMachineName()
                  .Enrich.WithExceptionDetails();

      if(testEnricher != null)
         config = config.Enrich.With(testEnricher);

      Log.Logger = config.MinimumLevel.Debug()
                         .WriteTo.Seq("http://192.168.0.11:5341", formatProvider: CultureInfo.InvariantCulture)
                         .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                         .CreateLogger();

      CompzeLogger.LoggerFactoryMethod = SerilogLogger.Create;
   }
}
