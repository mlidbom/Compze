using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.SystemCE.ThreadingCE;

interface ITaskRunner
{
   void Run(string taskName, Action task);
   void Run(string taskName, Func<unit> task);
   Thread RunOnNamedThread(string threadName, ThreadStart threadLoop, ThreadPriority priority = ThreadPriority.Normal);
}

static class TaskRunnerRegistrar
{
   internal static IComponentRegistrar TaskRunner(this IComponentRegistrar registrar)
      => registrar.Register(TaskRunnerImpl.RegisterWith);

   class TaskRunnerImpl : ITaskRunner, IDisposable
   {
      internal static void RegisterWith(IComponentRegistrar registrar)
         => registrar.Register(Singleton.For<ITaskRunner>().CreatedBy((IBackgroundExceptionReporter exceptionReporter) => new TaskRunnerImpl(exceptionReporter)));

      readonly IBackgroundExceptionReporter _exceptionReporter;
      readonly IThreadShared<List<Thread>> _threads = IThreadShared.WithDefaultTimeout(new List<Thread>());

      TaskRunnerImpl(IBackgroundExceptionReporter exceptionReporter) => _exceptionReporter = exceptionReporter;

      public void Run(string taskName, Action task)
      {
         TaskCE.Run(() =>
         {
            try
            {
               task();
            }
#pragma warning disable CA1031 //This is specifically designed for making sure that exceptions thrown in places where they cannot be surfaced directly, are not just ignored
            catch(Exception exception)
            {
#pragma warning restore CA1031
               _exceptionReporter.ReportException(exception);
            }
         });
      }

      public void Run(string taskName, Func<unit> task) => Run(taskName, () => { task(); });

      public Thread RunOnNamedThread(string threadName, ThreadStart threadLoop, ThreadPriority priority = ThreadPriority.Normal)
      {
         var thread = new Thread(() =>
         {
            try
            {
               threadLoop.Invoke();
            }
            catch(Exception exception) when(exception is OperationCanceledException or ThreadInterruptedException or ThreadAbortException)
            {
               this.Log().Info($"Thread: {threadName} is terminating because it received a: {exception.GetType().Name}.");
            }
#pragma warning disable CA1031 //This is specifically designed for making sure that exceptions thrown in places where they cannot be surfaced directly, are not just ignored
            catch(Exception exception)
            {
#pragma warning restore CA1031
               _exceptionReporter.ReportException(exception);
            }
         })
         {
            Name = threadName,
            Priority = priority
         };

         _threads.Update(it => it.Add(thread));
         thread.Start();
         return thread;
      }

      readonly CancellationTokenSource _cancellationTokenSource = new();

      public void Dispose()
      {
         var threads = _threads.Read(threads => threads.ToArray());
         foreach(var thread in threads)
         {
            if(thread.IsAlive)
            {
               thread.Interrupt();
            }
         }

         foreach(var thread in threads)
         {
            if(thread.IsAlive)
            {
               thread.Join();
            }
         }

         _cancellationTokenSource.Dispose();
      }
   }
}
