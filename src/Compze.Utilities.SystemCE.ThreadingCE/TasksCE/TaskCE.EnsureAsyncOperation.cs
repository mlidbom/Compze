using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Functional;

namespace Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

public static partial class TaskCE
{
   ///<summary>
   /// Like Task.Run, but this one guarantees that the task runs on a different thread from the caller, eliminating subtle and hard to debug problems in the case where Task.Run occasionally does NOT run on a different thread
   /// Also guarantees that any continuations are executed asynchronously rather than inline, another thing that occasionally may not be the case otherwise, again causing hard to debug issues.
   /// </summary>
   public static Task Run(Action action) => Run(action.AsFunc());

   ///<summary>
   /// Like Task.Run, but this one guarantees that the task runs on a different thread from the caller, eliminating subtle and hard to debug problems in the case where Task.Run occasionally does NOT run on a different thread
   /// Also guarantees that any continuations are executed asynchronously rather than inline, another thing that occasionally may not be the case otherwise, again causing hard to debug issues.
   /// </summary>
   public static Task<TResult> Run<TResult>(Func<TResult> function)
   {
      var taskCompletionSource = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

      ThreadPool.QueueUserWorkItem(_ =>
                                   {
                                      try
                                      {
                                         var result = function();
                                         taskCompletionSource.SetResult(result);
                                      }
#pragma warning disable CA1031 // this is correct exception handling for tasks
                                      catch(Exception ex)
                                      {
#pragma warning restore CA1031
                                         taskCompletionSource.SetException(ex);
                                      }
                                   },
                                   null);

      return taskCompletionSource.Task;
   }

   static readonly TaskFactory DefaultSchedulerDenyChildAttachTaskFactory = new(CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, TaskScheduler.Default);
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler We just did. On the line above...
   public static Task RunOnDedicatedThread(Action action) => RunOnDedicatedThread(action.AsFunc());
   public static Task<T> RunOnDedicatedThread<T>(Func<T> func) => DefaultSchedulerDenyChildAttachTaskFactory.StartNew(func, TaskCreationOptions.LongRunning);
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler

   ///<summary>Task.ContinueWith may run the continuation synchronously on the calling thread, causing all kinds of subtle and hard to debug problems. This simply guarantees that that does not happen.</summary>
   public static Task ContinueWithCE(this Task @this, Action<Task> continuation) =>
      @this.ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);
}
