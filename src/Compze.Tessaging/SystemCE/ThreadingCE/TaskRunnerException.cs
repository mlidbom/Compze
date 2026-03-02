using System;

namespace Compze.Tessaging.SystemCE.ThreadingCE;

class TaskRunnerException(Exception exception, string message) : Exception(message, exception);
