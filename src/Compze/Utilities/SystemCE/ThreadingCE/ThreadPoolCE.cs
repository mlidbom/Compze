using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Utilities.SystemCE.ThreadingCE;

static class ThreadPoolCE
{
   internal static void TryToEnsureSufficientIdleThreadsToRunTasksConcurrently(int threadCount)
   {
      for(var tries = 1; IdleThreads <= threadCount && tries < 5; tries++)
      {
         using var waitForAllThreadsToStart = new CountdownEvent(threadCount);
         Task.WaitAll(1.Through(threadCount).Select(_ => TaskCE.RunOnDedicatedThread(() =>
         {
            // ReSharper disable AccessToDisposedClosure
            waitForAllThreadsToStart.Signal(1);
            waitForAllThreadsToStart.Wait();
         })).ToArray());
      }
   }

   static int ExecutingThreads => MaxThreads - AvailableThreads;
   static int LiveThreads => ThreadPool.ThreadCount;
   static int IdleThreads => LiveThreads - ExecutingThreads;

   static int MaxThreads
   {
      get
      {
         ThreadPool.GetMaxThreads(out var maxThreads, out _);
         return maxThreads;
      }
   }

   static int AvailableThreads
   {
      get
      {
         ThreadPool.GetAvailableThreads(out var availableThreads, out _);
         return availableThreads;
      }
   }
}
