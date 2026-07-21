using Compze.SystemCE;
using System.Runtime.CompilerServices;

namespace Compze.Internals.Logging.Private;

abstract class LevelLogger(ILogger logger) : ILevelLogger
{
   protected ILogger Logger { get; } = logger;
   public abstract bool IsEnabled();
   public abstract Unit Log(string message, [CallerMemberName] string caller = "");
   public abstract Unit Log([InterpolatedStringHandlerArgument("")] ref LevelLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "");
}

class TraceLogger(ILogger logger) : LevelLogger(logger)
{
   public override bool IsEnabled() => Logger.IsEnabled(LogLevel.Trace);
   public override Unit Log(string message, [CallerMemberName] string caller = "") => Logger.Trace(message, caller);
   public override Unit Log([InterpolatedStringHandlerArgument("")] ref LevelLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled)
      {
         var (template, values) = handler.Build();
         Logger.Trace(template, values, caller);
      }
      return unit;
   }
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

class CriticalLogger(ILogger logger) : LevelLogger(logger)
{
   public override bool IsEnabled() => Logger.IsEnabled(LogLevel.Critical);
   public override Unit Log(string message, [CallerMemberName] string caller = "") => Logger.Critical(message, caller);
   public override Unit Log([InterpolatedStringHandlerArgument("")] ref LevelLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled)
      {
         var (template, values) = handler.Build();
         Logger.Critical(template, values, caller);
      }
      return unit;
   }
}
