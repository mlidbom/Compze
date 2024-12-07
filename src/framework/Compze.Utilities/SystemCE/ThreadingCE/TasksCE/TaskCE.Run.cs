using System;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable MethodSupportsCancellation
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler. We use our own factory instance that already specifies the Scheduler

namespace Compze.SystemCE.ThreadingCE.TasksCE;

static partial class TaskCE
{
   static readonly TaskFactory DefaultSchedulerDenyChildAttachTaskFactory = new(CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, TaskScheduler.Default);
   internal static Task RunOnDedicatedThread(Action action) => DefaultSchedulerDenyChildAttachTaskFactory.StartNew(action, TaskCreationOptions.LongRunning);
}