using Compze.SystemCE;

namespace Compze.Internals.SystemCE.ThreadingCE.TasksCE;

public static partial class TaskCE
{
   ///<summary>
   /// Like Task.Run, but this one guarantees that the task runs on a different thread from the caller, eliminating subtle and hard to debug problems in the case where Task.Run occasionally does NOT run on a different thread
   /// Also guarantees that any continuations are executed asynchronously rather than inline, another thing that occasionally may not be the case otherwise, again causing hard to debug issues.
   /// </summary>
   public static Task Run(Action action) => Run(action.ToFunc());

   ///<summary>Like <see cref="Run(Action)"/> for async work: starts <paramref name="asyncFunction"/> on a thread-pool thread —<br/>
   /// guaranteed not inline on the caller — and returns a task that completes when the whole async work does, not when its<br/>
   /// synchronous prefix returns.</summary>
   public static async Task Run(Func<Task> asyncFunction) => await (await Run<Task>(asyncFunction).caf()).caf();

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
   ///<summary>Runs <paramref name="action"/> on a thread of its own, not a thread-pool thread: for blocking work an async caller<br/>
   /// must await, such as a wait on a synchronization primitive that has no async form.</summary>
   ///<remarks>The thread pool keeps only about as many threads as the machine has processors and adds more slowly, about one<br/>
   /// or two a second, so every pool thread parked in a blocking wait leaves one fewer to run every async continuation in the<br/>
   /// process. A few parked at once and continuations wait seconds for a thread; this costs a thread instead.</remarks>
   public static Task RunOnDedicatedThread(Action action) => RunOnDedicatedThread(action.ToFunc());

   ///<summary>Like <see cref="RunOnDedicatedThread(Action)"/>, for blocking work with a result.</summary>
   public static Task<T> RunOnDedicatedThread<T>(Func<T> func) => DefaultSchedulerDenyChildAttachTaskFactory.StartNew(func, TaskCreationOptions.LongRunning);
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler

   ///<summary>Task.ContinueWith may run the continuation synchronously on the calling thread, causing all kinds of subtle and hard to debug problems. This simply guarantees that that does not happen.</summary>
   public static Task ContinueWithCE(this Task @this, Action<Task> continuation) =>
      @this.ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);
}
