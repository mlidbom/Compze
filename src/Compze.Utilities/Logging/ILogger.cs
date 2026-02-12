using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.Logging;

interface ILogger
{
   ILogger WithLogLevel(LogLevel level);
   unit Error(Exception exception, string? message = null);
   unit Warning(string message);
   unit Warning(Exception exception, string message);
   unit Info(string message);
   unit Debug(string message);

   unit NCrunch(string message) => Warning($"NCR:{message}");
}
