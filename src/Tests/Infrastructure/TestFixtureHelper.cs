using System.Globalization;
using Compze.Utilities.Logging;
using Compze.Utilities.Logging.Serilog;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;

namespace Compze.Tests.Infrastructure;

/// <summary>
/// Common test fixture setup/teardown logic
/// </summary>
public static class TestFixtureHelper
{
   public static void SetupSerilog(ILogEventEnricher? testEnricher)
   {
      var config = new LoggerConfiguration()
                  .Enrich.WithMachineName()
                  .Enrich.WithExceptionDetails();

      if(testEnricher != null)
         config = config.Enrich.With(testEnricher);

      config.Enrich.With(new ShortSourceContextEnricher());

      Log.Logger = config.MinimumLevel.Debug()
                         .WriteTo.Seq("http://192.168.0.11:5341", formatProvider: CultureInfo.InvariantCulture)
                         .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                         .CreateLogger();

      CompzeLogger.LogLevel = LogLevel.Warning;
      CompzeLogger.LoggerFactoryMethod = SerilogLogger.Create;
   }

   public class ShortSourceContextEnricher : ILogEventEnricher
   {
      public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
      {
         if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
         {
            var fullName = sourceContext.ToString().Trim('"');
            var shortName = fullName.Substring(fullName.LastIndexOf('.') + 1);
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("LoggingClass", shortName));
         }
      }
   }
}
