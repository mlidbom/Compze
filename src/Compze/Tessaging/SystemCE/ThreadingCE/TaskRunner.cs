using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using System;
using System.Threading;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.SystemCE.ThreadingCE;

interface ITaskRunner
{
   //Bug: We cannot just ignore exceptions because we log them. Maybe calling component should be notified about them somehow and get to decide what to do?. Maybe return a Task that calling code is responsible for checking the result of sooner or later?
   //Bug: Check out: TaskScheduler.UnobservedTaskException and if we should use it. Perhaps in EndpointHost.
   void RunSwallowAndLogExceptions(string taskName, Action task);
}

static class TaskRunnerRegistrar
{
   internal static IDependencyRegistrar TaskRunner(this IDependencyRegistrar registrar)
      => registrar.Register(TaskRunnerImpl.RegisterWith);

   class TaskRunnerImpl : ITaskRunner, IDisposable
   {
      internal static void RegisterWith(IDependencyRegistrar registrar)
         => registrar.Register(Singleton.For<ITaskRunner>().CreatedBy(() => new TaskRunnerImpl()));

      TaskRunnerImpl() {}

      public void RunSwallowAndLogExceptions(string taskName, Action task)
      {
         TaskCE.Run(() =>
         {
            try
            {
               task();
            }
            catch(Exception exception)
            {
               this.Log().Error(exception, "Exception thrown on background thread. ");
            }
         });
      }

      readonly CancellationTokenSource _cancellationTokenSource = new();

      public void Dispose() => _cancellationTokenSource.Dispose();
   }
}