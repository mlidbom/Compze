using Compze.SystemCE;
using Compze.Internals.Logging.Private;
using System.Runtime.CompilerServices;

namespace Compze.Internals.Logging;

// ReSharper disable UnusedMember.Global : These functions are very useful for debugging but only used occasionally. Let's keep them around for now.
// ReSharper disable UnusedType.Global

public interface ILevelLogger
{
   bool IsEnabled();
   Unit Log(string message, [CallerMemberName] string caller = "");
   Unit Log([InterpolatedStringHandlerArgument("")] ref LevelLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "");
}

public static class LevelLoggerILoggerExtensions
{
   public static ILevelLogger Trace(this ILogger @this) => new TraceLogger(@this);
   public static ILevelLogger Debug(this ILogger @this) => new DebugLogger(@this);
   public static ILevelLogger Info(this ILogger @this) => new InfoLogger(@this);
   public static ILevelLogger Warning(this ILogger @this) => new WarningLogger(@this);
   public static ILevelLogger Critical(this ILogger @this) => new CriticalLogger(@this);
}
