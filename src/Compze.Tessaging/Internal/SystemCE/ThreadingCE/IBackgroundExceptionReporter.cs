namespace Compze.Tessaging.Internal.SystemCE.ThreadingCE;

interface IBackgroundExceptionReporter
{
   void ReportException(Exception exception);
   void ThrowIfAnyExceptions();
}
