using System;
using System.Collections.Generic;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Tessaging.SystemCE.ThreadingCE;

static class BackgroundExceptionReporterRegistrar
{
   internal static IComponentRegistrar BackgroundExceptionReporter(this IComponentRegistrar registrar)
      => registrar.Register(BackgroundExceptionReporterImpl.RegisterWith);

   class BackgroundExceptionReporterImpl : IBackgroundExceptionReporter
   {
      internal static void RegisterWith(IComponentRegistrar registrar)
         => registrar.Register(Singleton.For<IBackgroundExceptionReporter>().CreatedBy(() => new BackgroundExceptionReporterImpl()));

      readonly IThreadShared<List<Exception>> _collectedExceptions = IThreadShared.WithDefaultTimeout(new List<Exception>());

      public void ReportException(Exception exception)
      {
         _collectedExceptions.Update(it => it.Add(exception));
         try
         {
            CompzeLogger.For<BackgroundExceptionReporterImpl>().Error(exception, "Exception thrown on background thread.");
         }
#pragma warning disable CA1031 //This is specifically designed for making sure that exceptions thrown in places where they cannot be surfaced directly, are not just ignored
         catch(Exception loggingException)
         {
#pragma warning restore CA1031
            _collectedExceptions.Update(it => it.Add(loggingException));
         }
      }

      public void ThrowIfAnyExceptions()
      {
         var exceptions = _collectedExceptions.Read(exceptions => exceptions.ToArray());
         if(exceptions.Length > 0)
         {
            throw new AggregateException("Exceptions were thrown on background threads during endpoint execution.", exceptions);
         }
      }
   }
}
