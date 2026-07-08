using Compze.SystemCE;
using System.Runtime.CompilerServices;

namespace Compze.Internals.Logging;

public interface ILogger
{
   ILogger WithLogLevel(LogLevel level);
   bool IsEnabled(LogLevel level);

   Unit Critical(string message, [CallerMemberName] string caller = "");
   Unit Critical(string template, object?[] values, [CallerMemberName] string caller = "");
   Unit Critical([InterpolatedStringHandlerArgument("")] ref CriticalLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "");
   Unit Critical(Exception exception, string message, [CallerMemberName] string caller = "");
   Unit Critical(Exception exception, string template, object?[] values, [CallerMemberName] string caller = "");
   Unit Critical(Exception exception, [InterpolatedStringHandlerArgument("")] ref CriticalLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "");

   Unit Error(Exception exception, string message, [CallerMemberName] string caller = "");
   Unit Error(Exception exception, string template, object?[] values, [CallerMemberName] string caller = "");
   Unit Error(Exception exception, [InterpolatedStringHandlerArgument("")] ref ErrorLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "");

   Unit Warning(string message, [CallerMemberName] string caller = "");
   Unit Warning(string template, object?[] values, [CallerMemberName] string caller = "");
   Unit Warning([InterpolatedStringHandlerArgument("")] ref WarningLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "");
   Unit Warning(Exception exception, string message, [CallerMemberName] string caller = "");
   Unit Warning(Exception exception, string template, object?[] values, [CallerMemberName] string caller = "");
   Unit Warning(Exception exception, [InterpolatedStringHandlerArgument("")] ref WarningLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "");

   Unit Info(string message, [CallerMemberName] string caller = "");
   Unit Info(string template, object?[] values, [CallerMemberName] string caller = "");
   Unit Info([InterpolatedStringHandlerArgument("")] ref InfoLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "");

   Unit Debug(string message, [CallerMemberName] string caller = "");
   Unit Debug(string template, object?[] values, [CallerMemberName] string caller = "");
   Unit Debug([InterpolatedStringHandlerArgument("")] ref DebugLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "");

   Unit Trace(string message, [CallerMemberName] string caller = "");
   Unit Trace(string template, object?[] values, [CallerMemberName] string caller = "");
   Unit Trace([InterpolatedStringHandlerArgument("")] ref TraceLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "");
}
