using Compze.SystemCE;
using System.Runtime.CompilerServices;

namespace Compze.Internals.Logging;

public interface ILogger
{
   ILogger WithLogLevel(LogLevel level);
   Unit Error(Exception exception, string? message = null, [CallerMemberName] string caller = "");
   Unit Warning(string message, [CallerMemberName] string caller = "");
   Unit Warning(Exception exception, string message, [CallerMemberName] string caller = "");
   Unit Info(string message, [CallerMemberName] string caller = "");
   Unit Debug(string message, [CallerMemberName] string caller = "");
}
