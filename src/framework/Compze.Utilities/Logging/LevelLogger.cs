using Compze.Functional;

namespace Compze.Logging;

// ReSharper disable UnusedMember.Global : These functions are very useful for debugging but only used occasionally. Let's keep them around for now.
// ReSharper disable UnusedType.Global

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
