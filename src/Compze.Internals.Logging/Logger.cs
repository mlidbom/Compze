using System.Runtime.CompilerServices;
using Compze.SystemCE;

namespace Compze.Internals.Logging;

public abstract class Logger : ILogger
{
   readonly LogLevel? _configuredLogLevel;

   LogLevel CurrentLevel => _configuredLogLevel ?? CompzeLogger.LogLevel;

   protected Logger() {}
   protected Logger(LogLevel? configuredLogLevel) => _configuredLogLevel = configuredLogLevel;

   ///<summary>The level this logger was pinned to via <see cref="WithLogLevel"/>, or null if it follows the global <see cref="CompzeLogger.LogLevel"/>. Subclasses preserve it when deriving a new instance (e.g. in <see cref="WithProperty"/>).</summary>
   protected LogLevel? ConfiguredLogLevel => _configuredLogLevel;

   public abstract ILogger WithLogLevel(LogLevel level);

   ///<summary>Returns a logger that attaches the structured property <paramref name="name"/>=<paramref name="value"/> to everything it logs. Used to correlate output — e.g. all lines of one flow carry its name.</summary>
   public abstract ILogger WithProperty(string name, object? value);

   public bool IsEnabled(LogLevel level) => CurrentLevel >= level;

   protected abstract void ErrorInternal(Exception exception, string template, object?[]? values, string caller);
   protected abstract void WarningInternal(Exception? exception, string template, object?[]? values, string caller);
   protected abstract void InfoInternal(string template, object?[]? values, string caller);
   protected abstract void DebugInternal(string template, object?[]? values, string caller);

   public Unit Error(Exception exception, string message, [CallerMemberName] string caller = "")
   {
      if(IsEnabled(LogLevel.Error) && !CompzeLogger.LoggingSuppressed)
      {
         ErrorInternal(exception, message, values: null, caller);
      }
      return unit;
   }

   public Unit Error(Exception exception, string template, object?[] values, [CallerMemberName] string caller = "")
   {
      if(IsEnabled(LogLevel.Error) && !CompzeLogger.LoggingSuppressed)
      {
         ErrorInternal(exception, template, values, caller);
      }
      return unit;
   }

   public Unit Error(Exception exception, [InterpolatedStringHandlerArgument("")] ref ErrorLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled)
      {
         var (template, values) = handler.Build();
         ErrorInternal(exception, template, values, caller);
      }
      return unit;
   }

   public Unit Warning(string message, [CallerMemberName] string caller = "")
   {
      if(IsEnabled(LogLevel.Warning) && !CompzeLogger.LoggingSuppressed)
      {
         WarningInternal(exception: null, message, values: null, caller);
      }
      return unit;
   }

   public Unit Warning(string template, object?[] values, [CallerMemberName] string caller = "")
   {
      if(IsEnabled(LogLevel.Warning) && !CompzeLogger.LoggingSuppressed)
      {
         WarningInternal(exception: null, template, values, caller);
      }
      return unit;
   }

   public Unit Warning([InterpolatedStringHandlerArgument("")] ref WarningLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled)
      {
         var (template, values) = handler.Build();
         WarningInternal(exception: null, template, values, caller);
      }
      return unit;
   }

   public Unit Warning(Exception exception, string message, [CallerMemberName] string caller = "")
   {
      if(IsEnabled(LogLevel.Warning) && !CompzeLogger.LoggingSuppressed)
      {
         WarningInternal(exception, message, values: null, caller);
      }
      return unit;
   }

   public Unit Warning(Exception exception, string template, object?[] values, [CallerMemberName] string caller = "")
   {
      if(IsEnabled(LogLevel.Warning) && !CompzeLogger.LoggingSuppressed)
      {
         WarningInternal(exception, template, values, caller);
      }
      return unit;
   }

   public Unit Warning(Exception exception, [InterpolatedStringHandlerArgument("")] ref WarningLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled)
      {
         var (template, values) = handler.Build();
         WarningInternal(exception, template, values, caller);
      }
      return unit;
   }

   public Unit Info(string message, [CallerMemberName] string caller = "")
   {
      if(IsEnabled(LogLevel.Info) && !CompzeLogger.LoggingSuppressed)
      {
         InfoInternal(message, values: null, caller);
      }
      return unit;
   }

   public Unit Info(string template, object?[] values, [CallerMemberName] string caller = "")
   {
      if(IsEnabled(LogLevel.Info) && !CompzeLogger.LoggingSuppressed)
      {
         InfoInternal(template, values, caller);
      }
      return unit;
   }

   public Unit Info([InterpolatedStringHandlerArgument("")] ref InfoLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled)
      {
         var (template, values) = handler.Build();
         InfoInternal(template, values, caller);
      }
      return unit;
   }

   public Unit Debug(string message, [CallerMemberName] string caller = "")
   {
      if(IsEnabled(LogLevel.Debug) && !CompzeLogger.LoggingSuppressed)
      {
         DebugInternal(message, values: null, caller);
      }
      return unit;
   }

   public Unit Debug(string template, object?[] values, [CallerMemberName] string caller = "")
   {
      if(IsEnabled(LogLevel.Debug) && !CompzeLogger.LoggingSuppressed)
      {
         DebugInternal(template, values, caller);
      }
      return unit;
   }

   public Unit Debug([InterpolatedStringHandlerArgument("")] ref DebugLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled)
      {
         var (template, values) = handler.Build();
         DebugInternal(template, values, caller);
      }
      return unit;
   }
}
