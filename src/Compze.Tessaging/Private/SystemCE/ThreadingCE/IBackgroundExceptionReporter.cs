namespace Compze.Tessaging.Private.SystemCE.ThreadingCE;

interface IBackgroundExceptionReporter
{
   void ReportException(Exception exception);
   void ThrowIfAnyExceptions();
}
