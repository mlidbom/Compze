using Composable.SystemCE;

namespace Composable.Logging;

interface ILevelLogger
{
   VoidCE Log(string message);
}

class DebugLogger(ILogger logger) : ILevelLogger
{
   public VoidCE Log(string message) => logger.Debug(message);
}

class InfoLogger(ILogger logger) : ILevelLogger
{
   public VoidCE Log(string message) => logger.Info(message);
}

class WarningLogger(ILogger logger) : ILevelLogger
{
   public VoidCE Log(string message) => logger.Warning(message);
}

static class LevelLoggerILoggerExtensions
{
   public static ILevelLogger Debug(this ILogger @this) => new DebugLogger(@this);
   public static ILevelLogger Info(this ILogger @this) => new InfoLogger(@this);
   public static ILevelLogger Warning(this ILogger @this) => new WarningLogger(@this);
}
