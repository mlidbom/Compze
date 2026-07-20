namespace Compze.Tessaging.Internal.SystemCE.ThreadingCE;

class TaskRunnerException(Exception exception, string message) : Exception(message, exception);
