using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.Threading.ResourceAccess;

namespace Compze.Tessaging.SystemCE.ThreadingCE;

static class BackgroundExceptionReporterRegistrar
{
   public static IComponentRegistrar BackgroundExceptionReporter(this IComponentRegistrar registrar)
      => registrar.Register(BackgroundExceptionReporterCore.RegisterWith);

   class BackgroundExceptionReporterCore : IBackgroundExceptionReporter
   {
      public static void RegisterWith(IComponentRegistrar registrar)
         => registrar.Register(Singleton.For<IBackgroundExceptionReporter>().CreatedBy(() => new BackgroundExceptionReporterCore()));

      readonly IThreadShared<List<Exception>> _collectedExceptions = IThreadShared.New(new List<Exception>());

      public void ReportException(Exception exception)
      {
         _collectedExceptions.Locked(it => it.Add(exception));
         try
         {
            CompzeLogger.For<BackgroundExceptionReporterCore>().Error(exception, "Exception thrown on background thread.");
         }
#pragma warning disable CA1031 //This is specifically designed for making sure that exceptions thrown in places where they cannot be surfaced directly, are not just ignored
         catch(Exception loggingException)
         {
#pragma warning restore CA1031
            _collectedExceptions.Locked(it => it.Add(loggingException));
         }
      }

      public void ThrowIfAnyExceptions()
      {
         var exceptions = _collectedExceptions.Locked(exceptions => exceptions.ToArray());
         if(exceptions.Length > 0)
         {
            throw new AggregateException("Exceptions were thrown on background threads during endpoint execution.", exceptions);
         }
      }
   }
}
