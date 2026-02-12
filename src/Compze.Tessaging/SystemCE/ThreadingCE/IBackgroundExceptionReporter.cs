using System;

namespace Compze.Tessaging.SystemCE.ThreadingCE;

public interface IBackgroundExceptionReporter
{
   void ReportException(Exception exception);
   void ThrowIfAnyExceptions();
}
