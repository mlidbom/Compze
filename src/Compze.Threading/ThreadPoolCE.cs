using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.Threading.TasksCE;
using Compze.Threading.Utilities;

namespace Compze.Threading;

public static class ThreadPoolCE
{
   public static void TryToEnsureSufficientIdleThreadsToRunTasksConcurrently(int concurrentTaskCount)
   {
      using var waitForAllThreadsToStart = new CountdownEvent(concurrentTaskCount);
      Task.WaitAll(1.Through(concurrentTaskCount).Select(_ => TaskCE.Run(() =>
      {
         // ReSharper disable AccessToDisposedClosure
         waitForAllThreadsToStart.Signal(1);
         waitForAllThreadsToStart.Wait();
      })).ToArray());
   }
}
