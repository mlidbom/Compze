using System.Runtime.CompilerServices;

namespace Compze.Utilities.Logging;

public interface ILogger
{
   ILogger WithLogLevel(LogLevel level);
   unit Error(Exception exception, string? message = null, [CallerMemberName] string caller = "");
   unit Warning(string message, [CallerMemberName] string caller = "");
   unit Warning(Exception exception, string message, [CallerMemberName] string caller = "");
   unit Info(string message, [CallerMemberName] string caller = "");
   unit Debug(string message, [CallerMemberName] string caller = "");
}
