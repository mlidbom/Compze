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
      ///<remarks>A thread of its own watches the deadline, and writes the dump the moment it passes. A timer's callback, such as<br/>
      /// <see cref="Task.WaitAsync(TimeSpan)"/>'s, runs on the thread pool, so while the pool is starved, which is often why the task<br/>
      /// is late, it would run only once the pool frees up, and the dump would show the process after the stall instead of in it.</remarks>
      public async Task WaitAsyncDumpingTheProcessOnTimeout(TimeSpan timeout, string whatIsAwaited)
      {
         var evidence = await TaskCE.RunOnDedicatedThreadAsync(() => ((IAsyncResult)@this).AsyncWaitHandle.WaitOne(timeout)
                                                                        ? null
                                                                        : new TimeoutEvidence(ThreadPoolCE.DescribeState(), TestProcessDump.Write($"timeout awaiting {whatIsAwaited}"))).caf();
         if(evidence is null)
         {
            await @this.caf();
            return;
         }

         throw new TimeoutException($"""
                                     {whatIsAwaited} did not complete within {timeout}.
                                     Thread pool when the timeout ran out: {evidence.ThreadPoolState}
                                     A dump of the process, taken while it was still waiting, was written to: {evidence.DumpFile.FullName}
                                     """);
      }
   }

   ///<summary>What a timeout leaves: the thread pool's state when it ran out, and the dump written then.</summary>
   record TimeoutEvidence(string ThreadPoolState, FileInfo DumpFile);
}
