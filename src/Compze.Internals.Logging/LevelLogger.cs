using Compze.SystemCE;
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

abstract class LevelLogger(ILogger logger) : ILevelLogger
{
   protected ILogger Logger { get; } = logger;
   public abstract bool IsEnabled();
   public abstract Unit Log(string message, [CallerMemberName] string caller = "");
   public abstract Unit Log([InterpolatedStringHandlerArgument("")] ref LevelLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "");
}

class DebugLogger(ILogger logger) : LevelLogger(logger)
{
   public override bool IsEnabled() => Logger.IsEnabled(LogLevel.Debug);
   public override Unit Log(string message, [CallerMemberName] string caller = "") => Logger.Debug(message, caller);
   public override Unit Log([InterpolatedStringHandlerArgument("")] ref LevelLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled)
      {
         var (template, values) = handler.Build();
         Logger.Debug(template, values, caller);
      }
      return unit;
   }
}

class InfoLogger(ILogger logger) : LevelLogger(logger)
{
   public override bool IsEnabled() => Logger.IsEnabled(LogLevel.Info);
   public override Unit Log(string message, [CallerMemberName] string caller = "") => Logger.Info(message, caller);
   public override Unit Log([InterpolatedStringHandlerArgument("")] ref LevelLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled)
      {
         var (template, values) = handler.Build();
         Logger.Info(template, values, caller);
      }
      return unit;
   }
}

class WarningLogger(ILogger logger) : LevelLogger(logger)
{
   public override bool IsEnabled() => Logger.IsEnabled(LogLevel.Warning);
   public override Unit Log(string message, [CallerMemberName] string caller = "") => Logger.Warning(message, caller);
   public override Unit Log([InterpolatedStringHandlerArgument("")] ref LevelLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled)
      {
         var (template, values) = handler.Build();
         Logger.Warning(template, values, caller);
      }
      return unit;
   }
}

public static class LevelLoggerILoggerExtensions
{
   public static ILevelLogger Debug(this ILogger @this) => new DebugLogger(@this);
   public static ILevelLogger Info(this ILogger @this) => new InfoLogger(@this);
   public static ILevelLogger Warning(this ILogger @this) => new WarningLogger(@this);
}
