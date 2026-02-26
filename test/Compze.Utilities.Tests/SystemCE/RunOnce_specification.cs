using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Utilities.Tests.SystemCE;

public class RunOnce_specification
{
   public class RunIfFirstCallAsync
   {
      [XF] public async Task executes_the_action_on_first_call()
      {
         var runOnce = new RunOnce();
         var executed = false;
         await runOnce.RunIfFirstCallAsync(async () =>
         {
            await Task.Yield();
            executed = true;
         });
         executed.Must().BeTrue();
      }

      [XF] public async Task does_not_execute_the_action_on_second_call()
      {
         var runOnce = new RunOnce();
         var executionCount = 0;
         await runOnce.RunIfFirstCallAsync(async () =>
         {
            await Task.Yield();
            Interlocked.Increment(ref executionCount);
         });
         await runOnce.RunIfFirstCallAsync(async () =>
         {
            await Task.Yield();
            Interlocked.Increment(ref executionCount);
         });
         executionCount.Must().Be(1);
      }

      [XF] public async Task concurrent_caller_waits_for_first_call_to_complete()
      {
         var runOnce = new RunOnce();
         var actionStarted = new TaskCompletionSource();
         var allowActionToComplete = new TaskCompletionSource();
         var actionCompleted = false;

         var firstCall = Task.Run(async () =>
         {
            await runOnce.RunIfFirstCallAsync(async () =>
            {
               actionStarted.SetResult();
               await allowActionToComplete.Task;
               actionCompleted = true;
            });
         });

         await actionStarted.Task;

         var secondCallCompleted = false;
         var secondCall = Task.Run(async () =>
         {
            await runOnce.RunIfFirstCallAsync(async () =>
            {
               await Task.Yield();
               throw new InvalidOperationException("Should not execute");
            });
            secondCallCompleted = true;
         });

         await Task.Delay(50);
         secondCallCompleted.Must().BeFalse();

         allowActionToComplete.SetResult();
         await firstCall;
         await secondCall;

         actionCompleted.Must().BeTrue();
         secondCallCompleted.Must().BeTrue();
      }

      [XF] public async Task concurrent_caller_receives_exception_from_first_call()
      {
         var runOnce = new RunOnce();
         var actionStarted = new TaskCompletionSource();

         var firstCall = Task.Run(async () =>
         {
            await runOnce.RunIfFirstCallAsync(async () =>
            {
               actionStarted.SetResult();
               await Task.Yield();
               throw new InvalidOperationException("Schema creation failed");
            });
         });

         await actionStarted.Task;

         var secondCallException = default(Exception);
         var secondCall = Task.Run(async () =>
         {
            try
            {
               await runOnce.RunIfFirstCallAsync(async () =>
               {
                  await Task.Yield();
                  throw new InvalidOperationException("Should not execute");
               });
            }
            catch(Exception ex)
            {
               secondCallException = ex;
            }
         });

         await secondCall;

         try { await firstCall; }
         catch { /* expected */ }

         secondCallException.Must().NotBeNull();
         secondCallException!.Message.Must().Contain("Schema creation failed");
      }
   }

   public class RunIfFirstCall_sync
   {
      [XF] public void executes_the_action_on_first_call()
      {
         var runOnce = new RunOnce();
         var executed = false;
         runOnce.RunIfFirstCall(() => executed = true);
         executed.Must().BeTrue();
      }

      [XF] public void does_not_execute_the_action_on_second_call()
      {
         var runOnce = new RunOnce();
         var executionCount = 0;
         runOnce.RunIfFirstCall(() => Interlocked.Increment(ref executionCount));
         runOnce.RunIfFirstCall(() => Interlocked.Increment(ref executionCount));
         executionCount.Must().Be(1);
      }

      [XF] public void concurrent_caller_waits_for_first_call_to_complete()
      {
         var runOnce = new RunOnce();
         var actionStarted = new ManualResetEventSlim(false);
         var allowActionToComplete = new ManualResetEventSlim(false);
         var actionCompleted = false;

         var firstCall = Task.Run(() =>
         {
            runOnce.RunIfFirstCall(() =>
            {
               actionStarted.Set();
               allowActionToComplete.Wait();
               actionCompleted = true;
            });
         });

         actionStarted.Wait();

         var secondCallCompleted = false;
         var secondCall = Task.Run(() =>
         {
            runOnce.RunIfFirstCall(() => throw new InvalidOperationException("Should not execute"));
            secondCallCompleted = true;
         });

         Thread.Sleep(50);
         secondCallCompleted.Must().BeFalse();

         allowActionToComplete.Set();
         firstCall.Wait();
         secondCall.Wait();

         actionCompleted.Must().BeTrue();
         secondCallCompleted.Must().BeTrue();
      }
   }
}
