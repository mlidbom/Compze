using Compze.Internals.SystemCE.ThreadingCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tests.Infrastructure.Diagnostics;

///<summary>Awaiting a task with a timeout that, when it runs out, leaves the evidence of why: a <see cref="TestProcessDump"/><br/>
/// taken while the task is still stuck, and the thread pool's state in the exception's message.</summary>
public static class TaskTimeoutDiagnosticsCE
{
   extension(Task @this)
   {
      ///<summary>Like <see cref="Task.WaitAsync(TimeSpan)"/>, except that when <paramref name="timeout"/> runs out before the task<br/>
      /// completes it writes a <see cref="TestProcessDump"/> before throwing the <see cref="TimeoutException"/>, whose message names the<br/>
      /// dump file and the thread pool's state at the moment the timeout ran out.</summary>
      public async Task WaitAsyncDumpingTheProcessOnTimeout(TimeSpan timeout, string whatIsAwaited)
      {
         if(await Task.WhenAny(@this, Task.Delay(timeout)).caf() == @this)
         {
            await @this.caf();
            return;
         }

         var threadPoolState = ThreadPoolCE.DescribeState();
         var dumpFile = TestProcessDump.Write($"timeout awaiting {whatIsAwaited}");
         throw new TimeoutException($"""
                                     {whatIsAwaited} did not complete within {timeout}.
                                     Thread pool when the timeout ran out: {threadPoolState}
                                     A dump of the process, taken while it was still waiting, was written to: {dumpFile.FullName}
                                     """);
      }
   }
}
