using Compze.Internals.Logging.Serilog._private;
using Serilog;

namespace Compze.Internals.Logging.Serilog;

///<summary>
/// Backends that write Compze's log lines to Serilog, as structured events. Choose one for the process by assigning it to
/// <see cref="CompzeLogger.Backend"/>: <c>CompzeLogger.Backend = SerilogLogBackend.Global;</c>
///</summary>
public static class SerilogLogBackend
{
   ///<summary>
   /// Writes to Serilog's static <see cref="Log.Logger"/>, looked up for each line: it makes no difference whether
   /// <see cref="Log.Logger"/> is assigned before or after this backend is chosen.
   ///</summary>
   public static ILogBackend Global { get; } = new LogBackendWritingToSerilog(() => Log.Logger);

   ///<summary>Writes to <paramref name="serilogLogger"/>, instead of to Serilog's static <see cref="Log.Logger"/>.</summary>
   public static ILogBackend WritingTo(global::Serilog.ILogger serilogLogger) => new LogBackendWritingToSerilog(() => serilogLogger);
}
