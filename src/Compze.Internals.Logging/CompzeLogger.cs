using Compze.Internals.Logging._private;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

// ReSharper disable UnusedMember.Global

namespace Compze.Internals.Logging;

///<summary>
/// Where code gets its loggers, and the settings every logger in the process shares: the <see cref="Backend"/> lines are
/// written to, the minimum <see cref="LogLevel"/>, and temporary suppression.
///</summary>
///<remarks>
/// Every logger looks up these settings each time it logs a line, so a logger can be made at any time — in a
/// <c>static readonly</c> field of a type touched long before the host configures logging — and still honor the settings
/// the host chooses later.
///</remarks>
public static class CompzeLogger
{
   ///<summary>The logger for <typeparamref name="T"/>, the type of <paramref name="me"/>: the same one <see cref="For{T}"/> returns.</summary>
   // ReSharper disable once UnusedParameter.Global removing the parameter would make it impossible to invoke this as an extension method :)
#pragma warning disable IDE0060 //Reviewed OK: removing the parameter would make it impossible to invoke this as an extension method :)
   public static ILogger Log<T>(this T me) => LoggerCache<T>.Logger;
#pragma warning restore IDE0060 // Remove unused parameter

   ///<summary>The logger for <typeparamref name="T"/>: made once per type and shared by every caller.</summary>
   public static ILogger For<T>() => LoggerCache<T>.Logger;

   ///<summary>A logger for <paramref name="loggingType"/>. It behaves exactly like the one <see cref="For{T}"/> returns for that type.</summary>
   public static ILogger For(Type loggingType) => new Logger(loggingType);

   ///<summary>
   /// Where every logger writes its lines: <see cref="ILogBackend.Console"/> until the host assigns another, such as a
   /// Serilog backend.
   ///</summary>
   ///<remarks>
   /// Loggers look this up for each line they write, so the host can assign it whenever it is ready: lines logged before
   /// that went to the backend current at the time, and every line after goes to the new one, whenever its logger was made.
   ///</remarks>
   public static ILogBackend Backend { get; set; } = ILogBackend.Console;

   static class LoggerCache<T>
   {
      // ReSharper disable once StaticFieldInGenericType
      internal static readonly ILogger Logger = new Logger(typeof(T));
   }

   // ReSharper disable once FieldCanBeMadeReadOnly.Global It is meant to be settable
   public static LogLevel LogLevel { get; set; } = LogLevel.Info;

   static readonly AsyncLocal<bool> LoggingSuppressedTemporarily = new();
   public static bool LoggingSuppressed => LoggingSuppressedTemporarily.Value;


   public static async Task SuppressLoggingWhileRunningAsync(Func<Task> action)
   {
      using(ScopedChange.Enter(() => LoggingSuppressedTemporarily.Value = true, () => LoggingSuppressedTemporarily.Value = false))
      {
         await action().caf();
      }
   }
}
