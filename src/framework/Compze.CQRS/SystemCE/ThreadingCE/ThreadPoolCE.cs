using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.SystemCE.LinqCE;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.SystemCE.ThreadingCE;

static class ThreadPoolCE
{
   const string FakeTaskName = $"{nameof(ThreadPoolCE)}_{nameof(TryToEnsureSufficientIdleThreadsToRunTasksConcurrently)}";
   internal static void TryToEnsureSufficientIdleThreadsToRunTasksConcurrently(int threadCount)
   {
      for(var tries = 1; Idle <= threadCount && tries < 5; tries++)
      {
         var waitForAllThreadsToStart = new CountdownEvent(threadCount);
         Task.WaitAll(1.Through(threadCount).Select(_ => TaskCE.Run(FakeTaskName, () =>
         {
            waitForAllThreadsToStart.Signal(1);
            waitForAllThreadsToStart.Wait();
         })).ToArray());
      }
   }

   static int Executing => Max - Available;
   static int Live => ThreadPool.ThreadCount;
   static int Idle => Live - Executing;

   static int Max
   {
      get
      {
         ThreadPool.GetMaxThreads(out var maxThreads, out _);
         return maxThreads;
      }
   }

   static int Available
   {
      get
      {
         ThreadPool.GetAvailableThreads(out var availableThreads, out _);
         return availableThreads;
      }
   }
}