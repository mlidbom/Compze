using System;

namespace Compze.Tessaging.SystemCE.ThreadingCE;

public static class IBackgroundExceptionReporterExtensions
{
   public static void RunAndReportAnyExceptions(this IBackgroundExceptionReporter @this, Action action)
   {
      try
      {
         action();
      }
      catch(Exception exception)
      {
         @this.ReportException(exception);
      }
   }
}
