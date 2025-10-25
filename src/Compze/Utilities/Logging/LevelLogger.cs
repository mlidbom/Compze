using Compze.Utilities.Functional;

namespace Compze.Utilities.Logging;

// ReSharper disable UnusedMember.Global : These functions are very useful for debugging but only used occasionally. Let's keep them around for now.
// ReSharper disable UnusedType.Global

interface ILevelLogger
{
   unit Log(string tessage);
}

abstract class LevelLogger(ILogger logger) : ILevelLogger
{
   protected readonly ILogger Logger = logger;
   public abstract unit Log(string tessage);
}

class DebugLogger(ILogger logger) : LevelLogger(logger)
{
   public override unit Log(string tessage) => Logger.Debug(tessage);
}

class InfoLogger(ILogger logger) : LevelLogger(logger)
{
   public override unit Log(string tessage) => Logger.Info(tessage);
}

class WarningLogger(ILogger logger) : LevelLogger(logger)
{
   public override unit Log(string tessage) => Logger.Warning(tessage);
}

static class LevelLoggerILoggerExtensions
{
   public static ILevelLogger Debug(this ILogger @this) => new DebugLogger(@this);
   public static ILevelLogger Info(this ILogger @this) => new InfoLogger(@this);
   public static ILevelLogger Warning(this ILogger @this) => new WarningLogger(@this);
}
