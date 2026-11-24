using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Threading.ResourceAccess;

namespace Compze.Tessaging._private.SystemCE.ThreadingCE;

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
         //refactor: This is where an observer's or a best-effort handler's failure ends up, and an application has no public way
         //   to learn of it as it happens: it goes to Compze's internal log, and is thrown as an AggregateException only when an
         //   endpoint is disposed; a container with the LocalTessagingEngine alone never throws it. A public hook, such as an
         //   injectable reporter, would let an application alert on them. (website-and-docs, 2026-11-24)
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
