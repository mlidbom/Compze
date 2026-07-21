namespace Compze.Tessaging._private.SystemCE.ThreadingCE;

static class BackgroundExceptionReporterExtensions
{
   public static void RunSwallowingAndReportingAnyExceptions(this IBackgroundExceptionReporter @this, Action action)
   {
      try
      {
         action();
      }
#pragma warning disable CA1031 //This is specifically designed for making sure that exceptions thrown in places where they cannot be surfaced directly, are not just ignored
      catch(Exception exception)
      {
#pragma warning restore CA1031
         @this.ReportException(exception);
      }
   }
}
