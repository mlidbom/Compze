namespace Compze.ServiceBus.SystemCE.ThreadingCE;

interface IBackgroundExceptionReporter
{
   void ReportException(Exception exception);
   void ThrowIfAnyExceptions();
}
