namespace Compze.ServiceBus.SystemCE.ThreadingCE;

class TaskRunnerException(Exception exception, string message) : Exception(message, exception);
