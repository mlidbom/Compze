using System.Runtime.CompilerServices;
using Compze.Functional;

namespace Compze.Utilities.Logging;

// ReSharper disable UnusedMember.Global : These functions are very useful for debugging but only used occasionally. Let's keep them around for now.
// ReSharper disable UnusedType.Global

public interface ILevelLogger
{
   unit Log(string message, [CallerMemberName] string caller = "");
}

public abstract class LevelLogger(ILogger logger) : ILevelLogger
{
   protected ILogger Logger { get; } = logger;
   public abstract unit Log(string message, [CallerMemberName] string caller = "");
}

public class DebugLogger(ILogger logger) : LevelLogger(logger)
{
   public override unit Log(string message, [CallerMemberName] string caller = "") => Logger.Debug(message, caller);
}

public class InfoLogger(ILogger logger) : LevelLogger(logger)
{
   public override unit Log(string message, [CallerMemberName] string caller = "") => Logger.Info(message, caller);
}

public class WarningLogger(ILogger logger) : LevelLogger(logger)
{
   public override unit Log(string message, [CallerMemberName] string caller = "") => Logger.Warning(message, caller);
}

public static class LevelLoggerILoggerExtensions
{
   public static ILevelLogger Debug(this ILogger @this) => new DebugLogger(@this);
   public static ILevelLogger Info(this ILogger @this) => new InfoLogger(@this);
   public static ILevelLogger Warning(this ILogger @this) => new WarningLogger(@this);
}
