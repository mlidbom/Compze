using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.Logging;

interface ILogger
{
   ILogger WithLogLevel(LogLevel level);
   unit Error(Exception exception, string? tessage = null);
   unit Warning(string tessage);
   unit Warning(Exception exception, string tessage);
   unit Info(string tessage);
   unit Debug(string tessage);

   unit NCrunch(string tessage) => Warning($"NCR:{tessage}");
}
