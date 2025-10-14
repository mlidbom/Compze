using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Compze.Utilities.Functional;
using Compze.Utilities.Threading.ResourceAccess;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.SystemCE.ThreadingCE;

interface ITaskRunner
{
   void Run(string taskName, Action task);
   void Run(string taskName, Func<unit> task);
   void ThrowIfAnyExceptions();
}

static class TaskRunnerRegistrar
{
   internal static IDependencyRegistrar TaskRunner(this IDependencyRegistrar registrar)
      => registrar.Register(TaskRunnerImpl.RegisterWith);

   class TaskRunnerImpl : ITaskRunner, IDisposable
   {
      internal static void RegisterWith(IDependencyRegistrar registrar)
         => registrar.Register(Singleton.For<ITaskRunner>().CreatedBy(() => new TaskRunnerImpl()));

      readonly IThreadShared<List<Exception>> _collectedExceptions = ThreadShared.WithDefaultTimeout(new List<Exception>());

      TaskRunnerImpl() {}

      public void Run(string taskName, Action task)
      {
         TaskCE.Run(() =>
         {
            try
            {
               task();
            }
            catch(Exception exception)
            {
               _collectedExceptions.Update(it => it.Add(exception));
               try
               {
                  this.Log().Error(exception, "Exception thrown on background thread.");
               }
               catch(Exception loggingException)
               {
                  _collectedExceptions.Update(it => it.Add(loggingException));
               }
            }
         });
      }

      public void Run(string taskName, Func<unit> task) => Run(taskName, () => { task(); });

      public void ThrowIfAnyExceptions()
      {
         var exceptions = _collectedExceptions.Read(exceptions => exceptions.ToArray());
         if(exceptions.Length > 0)
         {
            throw new AggregateException("Exceptions were thrown on background threads during endpoint execution.", exceptions);
         }
      }

      readonly CancellationTokenSource _cancellationTokenSource = new();

      public void Dispose() => _cancellationTokenSource.Dispose();
   }
}
