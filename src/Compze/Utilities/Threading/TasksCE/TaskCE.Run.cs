using System;
using System.Threading;
using System.Threading.Tasks;

namespace Compze.Utilities.Threading.TasksCE;

static partial class TaskCE
{
   ///<summary>Like Task.Run, but this one guarantees that the task runs on a different thread, eliminating subtle and hard to debug problems in the case where TaskRun occasionally does NOT run on a different thread</summary>
   public static Task Run(Action action) => Run(action.AsUnitFunc());


   static readonly TaskFactory DefaultSchedulerDenyChildAttachTaskFactory = new(CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, TaskScheduler.Default);
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler We just did. On the line above...
   internal static Task RunOnDedicatedThread(Action action) => DefaultSchedulerDenyChildAttachTaskFactory.StartNew(action, TaskCreationOptions.LongRunning);
   internal static Task<T> RunOnDedicatedThread<T>(Func<T> func) => DefaultSchedulerDenyChildAttachTaskFactory.StartNew(func, TaskCreationOptions.LongRunning);
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler

   //internal static Task<T> Run<T>(Func<T> func) => DefaultSchedulerDenyChildAttachTaskFactory.StartNew(func, TaskCreationOptions.LongRunning);

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
