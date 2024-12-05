using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Logging;
using JetBrains.Annotations;

namespace Compze.SystemCE.ThreadingCE;

interface ITaskRunner
{
   //Bug: We cannot just ignore exceptions because we log them. Maybe calling component should be notified about them somehow and get to decide what to do?. Maybe return a Task that calling code is responsible for checking the result of sooner or later?
   //Bug: Check out: TaskScheduler.UnobservedTaskException and if we should use it. Perhaps in EndpointHost.
   void RunSwallowAndLogExceptions(string taskName, Action task);
}

[UsedImplicitly] class TaskRunner : ITaskRunner, IDisposable
{
   public void RunSwallowAndLogExceptions(string taskName, Action task)
   {
      Task.Run(() =>
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