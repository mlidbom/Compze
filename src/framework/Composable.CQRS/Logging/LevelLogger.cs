using Composable.Functional;
using Composable.SystemCE;
using Composable.SystemCE.LinqCE;

namespace Composable.Logging;

interface ILevelLogger
{
   Unit Log(string message);
}

abstract class LevelLogger(ILogger logger) : ILevelLogger
{
   protected readonly ILogger Logger = logger;
   public abstract Unit Log(string message);
}

class DebugLogger(ILogger logger) : LevelLogger(logger)
{
   public override Unit Log(string message) => Logger.Debug(message);
}

class InfoLogger(ILogger logger) : LevelLogger(logger)
{
   public override Unit Log(string message) => Logger.Info(message);
}

class WarningLogger(ILogger logger) : LevelLogger(logger)
{
   public override Unit Log(string message) => Logger.Warning(message);
}

static class LevelLoggerILoggerExtensions
{
   public static ILevelLogger Debug(this ILogger @this) => new DebugLogger(@this);
   public static ILevelLogger Info(this ILogger @this) => new InfoLogger(@this);
   public static ILevelLogger Warning(this ILogger @this) => new WarningLogger(@this);
}
