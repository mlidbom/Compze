using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.Logging;

interface ILogger
{
   ILogger WithLogLevel(LogLevel level);
   Unit Error(Exception exception, string? message = null);
   Unit Warning(string message);
   Unit Warning(Exception exception, string message);
   Unit Info(string message);
   Unit Debug(string message);
}
