using System;

namespace Compze.Tessaging.SystemCE.ThreadingCE;

internal interface IBackgroundExceptionReporter
{
   void ReportException(Exception exception);
   void ThrowIfAnyExceptions();
}
