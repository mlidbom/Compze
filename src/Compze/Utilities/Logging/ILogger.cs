using System;

namespace Compze.Utilities.Logging;

interface ILogger
{
   ILogger WithLogLevel(LogLevel level);
   unit Error(Exception exception, string? message = null);
   unit Warning(string message);
   unit Warning(Exception exception, string message);
   unit Info(string message);
   unit Debug(string message);
}
