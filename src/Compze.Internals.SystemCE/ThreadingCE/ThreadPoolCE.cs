namespace Compze.Internals.SystemCE.ThreadingCE;

///<summary>Helpers for the .NET <see cref="ThreadPool"/>.</summary>
public static class ThreadPoolCE
{
   ///<summary>The thread pool's state right now, in one line for an exception message or a log: its threads, its pending and completed work items,<br/>
   /// its worker and IO thread limits and how many are free, and the processor count.</summary>
   ///<remarks>For a wait that timed out, where the question is whether the pool starved: no free workers with work pending says the work the wait<br/>
   /// waited for couldn't get a thread to run on, while a pool with free workers says the cause was elsewhere.</remarks>
   public static string DescribeState()
   {
      ThreadPool.GetMinThreads(out var minimumWorkerThreads, out _);
      ThreadPool.GetAvailableThreads(out var freeWorkerThreads, out var freeIoThreads);
      ThreadPool.GetMaxThreads(out var maximumWorkerThreads, out var maximumIoThreads);
      return $"{ThreadPool.ThreadCount} threads, {ThreadPool.PendingWorkItemCount} pending work items, {ThreadPool.CompletedWorkItemCount} completed work items; "
           + $"worker threads: minimum {minimumWorkerThreads}, maximum {maximumWorkerThreads}, free {freeWorkerThreads}; "
           + $"IO threads: maximum {maximumIoThreads}, free {freeIoThreads}; processor count {Environment.ProcessorCount}";
   }
}
