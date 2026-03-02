using System;

namespace Compze.Tessaging.SystemCE.ThreadingCE;

interface IBackgroundExceptionReporter
{
   void ReportException(Exception exception);
   void ThrowIfAnyExceptions();
}
