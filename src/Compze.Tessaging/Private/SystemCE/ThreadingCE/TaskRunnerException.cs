namespace Compze.Tessaging.Private.SystemCE.ThreadingCE;

class TaskRunnerException(Exception exception, string message) : Exception(message, exception);
