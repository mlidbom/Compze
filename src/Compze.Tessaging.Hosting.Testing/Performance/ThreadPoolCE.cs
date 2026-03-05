using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Hosting.Testing.Performance;

static class ThreadPoolCE
{
   public static void TryToEnsureSufficientIdleThreadsToRunTasksConcurrently(int concurrentTaskCount)
   {
      using var waitForAllThreadsToStart = new CountdownEvent(concurrentTaskCount);
      Task.WaitAll(Enumerable.Range(1, concurrentTaskCount).Select(_ => TaskCE.Run(() =>
      {
         // ReSharper disable AccessToDisposedClosure
         waitForAllThreadsToStart.Signal(1);
         waitForAllThreadsToStart.Wait();
      })).ToArray());
   }
}
