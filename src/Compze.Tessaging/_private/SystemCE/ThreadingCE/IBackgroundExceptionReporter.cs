namespace Compze.Tessaging._private.SystemCE.ThreadingCE;

interface IBackgroundExceptionReporter
{
   void ReportException(Exception exception);
   void ThrowIfAnyExceptions();
}
