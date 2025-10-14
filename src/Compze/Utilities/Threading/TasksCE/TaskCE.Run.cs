using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable MethodSupportsCancellation
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler. We use our own factory instance that already specifies the Scheduler

namespace Compze.Utilities.Threading.TasksCE;

static partial class TaskCE
{
   static readonly TaskFactory DefaultSchedulerDenyChildAttachTaskFactory = new(CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, TaskScheduler.Default);

   ///<summary>Like Task.Run, but this one guarantees that the task runs on a different thread, eliminating subtle and hard to debug problems in the case where TaskRun occasionally does NOT run on a different thread</summary>
   public static Task Run(Action action) => Run(action.AsUnitFunc());

   ///<summary>Like Task.Run, but this one guarantees that the task runs on a different thread, eliminating subtle and hard to debug problems in the case where TaskRun occasionally does NOT run on a different thread</summary>
   public static Task<TResult> Run<TResult>(Func<TResult> function)
   {
      var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

      ThreadPool.QueueUserWorkItem(_ =>
                                   {
                                      try
                                      {
                                         var result = function();
                                         tcs.SetResult(result);
                                      }
                                      catch(Exception ex)
                                      {
                                         tcs.SetException(ex);
                                      }
                                   },
                                   null);

      return tcs.Task;
   }
}
