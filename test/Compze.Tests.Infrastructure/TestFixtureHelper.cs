using System.Globalization;
using Compze.Utilities.Logging;
using Compze.Utilities.Logging.Serilog;
using Compze.Utilities.SystemCE;
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
   internal static void SetupSerilog(ILogEventEnricher? testEnricher, ILogEventSink? testOutputSink = null)
   {
      var config = new LoggerConfiguration()
                  .Enrich.WithMachineName()
                  .Enrich.WithExceptionDetails();

      if(testEnricher != null)
         config = config.Enrich.With(testEnricher);

      config.Enrich.With(new ShortSourceContextEnricher());

      var loggerConfig = config.MinimumLevel.Debug();

      if(!CompzeEnvironment.IsGithubAction)
      {
         loggerConfig = loggerConfig.WriteTo.Seq("http://192.168.0.11:5341", formatProvider: CultureInfo.InvariantCulture)
                                    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
      }

      if(testOutputSink != null)
         loggerConfig = loggerConfig.WriteTo.Sink(testOutputSink);

      Log.Logger = loggerConfig.CreateLogger();

      CompzeLogger.LogLevel = CompzeEnvironment.IsGithubAction ? LogLevel.Debug : LogLevel.Warning;
      CompzeLogger.LoggerFactoryMethod = SerilogLogger.Create;
   }

   class ShortSourceContextEnricher : ILogEventEnricher
   {
      public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
      {
         if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
         {
            var fullName = sourceContext.ToString().Trim('"');
            var shortName = fullName.Substring(fullName.LastIndexOf('.') + 1);

            var callerMember = "";
            if (logEvent.Properties.TryGetValue("CallerMember", out var callerProp))
               callerMember = callerProp.ToString().Trim('"');

            var logSource = LogSourceFormatter.Format(shortName, callerMember);
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("LogSource", logSource));
         }
      }
   }
}
