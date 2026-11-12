using Compze.SystemCE;
using Compze.Internals.Logging._private;
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
   extension(ILogger @this)
   {
      public ILevelLogger Trace() => new TraceLogger(@this);
      public ILevelLogger Debug() => new DebugLogger(@this);
      public ILevelLogger Info() => new InfoLogger(@this);
      public ILevelLogger Warning() => new WarningLogger(@this);
      public ILevelLogger Error() => new ErrorLogger(@this);
      public ILevelLogger Critical() => new CriticalLogger(@this);
   }
}
