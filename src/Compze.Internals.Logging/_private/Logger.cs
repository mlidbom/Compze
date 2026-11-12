using System.Runtime.CompilerServices;
using Compze.SystemCE;

namespace Compze.Internals.Logging._private;

///<summary>
/// The <see cref="ILogger"/> that <see cref="CompzeLogger"/> hands out: a logger for one <see cref="Type"/>, optionally at a
/// <see cref="LogLevel"/> of its own. It decides whether a line is written, and hands each line it writes to the
/// <see cref="CompzeLogger.Backend"/> current at that moment.
///</summary>
///<remarks>
/// It holds no backend, so a logger made before the host chose its backend still writes to that backend, and a logger can
/// live in a <c>static readonly</c> field of a type that is touched before logging is configured.
///</remarks>
sealed class Logger : ILogger
{
   readonly Type _sourceType;
   readonly LogLevel? _ownLogLevel;

   internal Logger(Type sourceType) => _sourceType = sourceType;
   Logger(Type sourceType, LogLevel ownLogLevel) : this(sourceType) => _ownLogLevel = ownLogLevel;

   LogLevel CurrentLevel => _ownLogLevel ?? CompzeLogger.LogLevel;

   public ILogger WithLogLevel(LogLevel level) => new Logger(_sourceType, level);

   public bool IsEnabled(LogLevel level) => level >= CurrentLevel;

   public Unit Critical(string message, [CallerMemberName] string caller = "") => WritePlainTextIfEnabled(LogLevel.Critical, exception: null, message, caller);
   public Unit Critical(string template, object?[] values, [CallerMemberName] string caller = "") => WriteTemplateIfEnabled(LogLevel.Critical, exception: null, template, values, caller);
   public Unit Critical(Exception exception, string message, [CallerMemberName] string caller = "") => WritePlainTextIfEnabled(LogLevel.Critical, exception, message, caller);
   public Unit Critical(Exception exception, string template, object?[] values, [CallerMemberName] string caller = "") => WriteTemplateIfEnabled(LogLevel.Critical, exception, template, values, caller);

   public Unit Critical([InterpolatedStringHandlerArgument("")] ref CriticalLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled) Write(LogLevel.Critical, exception: null, handler.Build(), caller);
      return unit;
   }

   public Unit Critical(Exception exception, [InterpolatedStringHandlerArgument("")] ref CriticalLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled) Write(LogLevel.Critical, exception, handler.Build(), caller);
      return unit;
   }

   public Unit Error(string message, [CallerMemberName] string caller = "") => WritePlainTextIfEnabled(LogLevel.Error, exception: null, message, caller);
   public Unit Error(string template, object?[] values, [CallerMemberName] string caller = "") => WriteTemplateIfEnabled(LogLevel.Error, exception: null, template, values, caller);
   public Unit Error(Exception exception, string message, [CallerMemberName] string caller = "") => WritePlainTextIfEnabled(LogLevel.Error, exception, message, caller);
   public Unit Error(Exception exception, string template, object?[] values, [CallerMemberName] string caller = "") => WriteTemplateIfEnabled(LogLevel.Error, exception, template, values, caller);

   public Unit Error([InterpolatedStringHandlerArgument("")] ref ErrorLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled) Write(LogLevel.Error, exception: null, handler.Build(), caller);
      return unit;
   }

   public Unit Error(Exception exception, [InterpolatedStringHandlerArgument("")] ref ErrorLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled) Write(LogLevel.Error, exception, handler.Build(), caller);
      return unit;
   }

   public Unit Warning(string message, [CallerMemberName] string caller = "") => WritePlainTextIfEnabled(LogLevel.Warning, exception: null, message, caller);
   public Unit Warning(string template, object?[] values, [CallerMemberName] string caller = "") => WriteTemplateIfEnabled(LogLevel.Warning, exception: null, template, values, caller);
   public Unit Warning(Exception exception, string message, [CallerMemberName] string caller = "") => WritePlainTextIfEnabled(LogLevel.Warning, exception, message, caller);
   public Unit Warning(Exception exception, string template, object?[] values, [CallerMemberName] string caller = "") => WriteTemplateIfEnabled(LogLevel.Warning, exception, template, values, caller);

   public Unit Warning([InterpolatedStringHandlerArgument("")] ref WarningLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled) Write(LogLevel.Warning, exception: null, handler.Build(), caller);
      return unit;
   }

   public Unit Warning(Exception exception, [InterpolatedStringHandlerArgument("")] ref WarningLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled) Write(LogLevel.Warning, exception, handler.Build(), caller);
      return unit;
   }

   public Unit Info(string message, [CallerMemberName] string caller = "") => WritePlainTextIfEnabled(LogLevel.Info, exception: null, message, caller);
   public Unit Info(string template, object?[] values, [CallerMemberName] string caller = "") => WriteTemplateIfEnabled(LogLevel.Info, exception: null, template, values, caller);

   public Unit Info([InterpolatedStringHandlerArgument("")] ref InfoLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled) Write(LogLevel.Info, exception: null, handler.Build(), caller);
      return unit;
   }

   public Unit Debug(string message, [CallerMemberName] string caller = "") => WritePlainTextIfEnabled(LogLevel.Debug, exception: null, message, caller);
   public Unit Debug(string template, object?[] values, [CallerMemberName] string caller = "") => WriteTemplateIfEnabled(LogLevel.Debug, exception: null, template, values, caller);

   public Unit Debug([InterpolatedStringHandlerArgument("")] ref DebugLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled) Write(LogLevel.Debug, exception: null, handler.Build(), caller);
      return unit;
   }

   public Unit Trace(string message, [CallerMemberName] string caller = "") => WritePlainTextIfEnabled(LogLevel.Trace, exception: null, message, caller);
   public Unit Trace(string template, object?[] values, [CallerMemberName] string caller = "") => WriteTemplateIfEnabled(LogLevel.Trace, exception: null, template, values, caller);

   public Unit Trace([InterpolatedStringHandlerArgument("")] ref TraceLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "")
   {
      if(handler.Enabled) Write(LogLevel.Trace, exception: null, handler.Build(), caller);
      return unit;
   }

   bool WritesAt(LogLevel level) => IsEnabled(level) && !CompzeLogger.LoggingSuppressed;

   ///<summary>Writes <paramref name="text"/> as literal text: its braces are doubled, so no backend reads them as holes.</summary>
   Unit WritePlainTextIfEnabled(LogLevel level, Exception? exception, string text, string caller)
   {
      if(WritesAt(level)) Write(level, exception, (MessageTemplateFor(text), []), caller);
      return unit;
   }

   Unit WriteTemplateIfEnabled(LogLevel level, Exception? exception, string template, object?[] values, string caller)
   {
      if(WritesAt(level)) Write(level, exception, (template, values), caller);
      return unit;
   }

   void Write(LogLevel level, Exception? exception, (string template, object?[] values) message, string caller)
      => CompzeLogger.Backend.Write(new LogLine(level, _sourceType, caller, message.template, message.values, exception));

   static string MessageTemplateFor(string literalText) =>
      literalText.ContainsOrdinal('{') || literalText.ContainsOrdinal('}')
         ? literalText.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal)
         : literalText;
}
