namespace Compze.Tessaging._private.SystemCE.ThreadingCE;

class TaskRunnerException(Exception exception, string message) : Exception(message, exception);
