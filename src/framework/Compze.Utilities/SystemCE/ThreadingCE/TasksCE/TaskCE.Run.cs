using System;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable MethodSupportsCancellation
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler. We use our own factory instance that already specifies the Scheduler

namespace Compze.SystemCE.ThreadingCE.TasksCE;

static partial class TaskCE
{
   static readonly TaskFactory DefaultSchedulerDenyChildAttachTaskFactory = new(CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, TaskScheduler.Default);

   ///<summary>Passing <see cref="TaskCreationOptions.LongRunning"/> to <see cref="TaskFactory{TResult}.StartNew(System.Func{object?,TResult},object?)"/> which has the result of ensuring that the task gets a thread to run on right away.</summary>
   internal static Task RunPrioritized(Action action) => DefaultSchedulerDenyChildAttachTaskFactory.StartNew(action, TaskCreationOptions.LongRunning);
}