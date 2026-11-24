using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Internals.Testing.Performance._private;

static class ThreadPoolCE
{
   public static void TryToEnsureSufficientIdleThreadsToRunTasksConcurrently(int concurrentTaskCount)
   {
      using var waitForAllThreadsToStart = new CountdownEvent(concurrentTaskCount);
#pragma warning disable VSTHRD002 // Holding thread-pool threads until the pool has added enough of them is what this does.
      Task.WaitAll(Enumerable.Range(1, concurrentTaskCount).Select(_ => TaskCE.RunAsync(() =>
      {
         // ReSharper disable AccessToDisposedClosure
         waitForAllThreadsToStart.Signal(1);
         waitForAllThreadsToStart.Wait();
      })).ToArray());
#pragma warning restore VSTHRD002
   }
}
