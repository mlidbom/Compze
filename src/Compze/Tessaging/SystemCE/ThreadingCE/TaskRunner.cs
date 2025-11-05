using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.LinqCE;
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
      readonly IThreadShared<HashSet<Task>> _inProgressTasks = IThreadShared.WithDefaultTimeout(new HashSet<Task>());

      TaskRunnerImpl(IBackgroundExceptionReporter exceptionReporter) => _exceptionReporter = exceptionReporter;

      public void Run(string taskName, Action task)
      {
         var newTask = TaskCE.Run(() =>
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

         _inProgressTasks.Update(it => it.Add(newTask));
         newTask.ContinueWith(completedTask => _inProgressTasks.Update(it => it.Remove(newTask)));//While surprising to me, completedTask and newTask are NOT the same object.
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
         threads.Where(it => it.IsAlive)
                .ForEach(it => it.Interrupt());
         threads.Where(it => it.IsAlive)
                .ForEach(it => it.Join());
         _inProgressTasks.Await(it => !it.Any());

         _cancellationTokenSource.Dispose();
      }
   }
}
