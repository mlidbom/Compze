using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Compze.Threading;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using Xunit;

namespace Compze.Utilities.Tests.SystemCE.ThreadingCE;

#pragma warning disable CA1052 // Static holder class used for test organization
public class RunOnceAsync_specification
{
   public class RunIfFirstCallAsync
   {
      [XF] public async Task Executes_the_action_on_first_call()
      {
         var runOnce = new RunOnceAsync();
         var executed = false;
         await runOnce.RunIfFirstCallAsync(async () =>
         {
            await Task.Yield();
            executed = true;
         });
         executed.Must().BeTrue();
      }

      [XF] public async Task Does_not_execute_the_action_on_second_call()
      {
         var runOnce = new RunOnceAsync();
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

      [XF] public async Task Concurrent_caller_waits_for_first_call_to_complete()
      {
         var runOnce = new RunOnceAsync();
         var actionStarted = new TaskCompletionSource();
         var allowActionToComplete = new TaskCompletionSource();
         var events = new ConcurrentQueue<string>();

         var firstCall = Task.Run(async () =>
         {
            await runOnce.RunIfFirstCallAsync(async () =>
            {
               actionStarted.SetResult();
               await allowActionToComplete.Task;
               events.Enqueue("first_completed");
            });
         });

         await actionStarted.Task;

         var secondCall = Task.Run(async () =>
         {
            await runOnce.RunIfFirstCallAsync(async () =>
            {
               await Task.Yield();
               throw new InvalidOperationException("Should not execute");
            });
            events.Enqueue("second_returned");
         });

         allowActionToComplete.SetResult();
         await firstCall;
         await secondCall;

         events.ToArray().Must().SequenceEqual(["first_completed", "second_returned"]);
      }

      [XF] public async Task Concurrent_caller_receives_exception_from_first_call()
      {
         var runOnce = new RunOnceAsync();
         var actionStarted = new TaskCompletionSource();

         var firstCall = Task.Run(async () =>
         {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
               await runOnce.RunIfFirstCallAsync(async () =>
               {
                  actionStarted.SetResult();
                  await Task.Yield();
                  throw new InvalidOperationException("Schema creation failed");
               }));
         });

         await actionStarted.Task;

         var secondCall = Task.Run(async () =>
         {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
               await runOnce.RunIfFirstCallAsync(async () =>
               {
                  await Task.Yield();
                  throw new InvalidOperationException("Should not execute");
               }));
            ex.Message.Must().Contain("Schema creation failed");
         });

         await firstCall;
         await secondCall;
      }
   }

   public class IsFirstCall
   {
      [XF] public void Returns_true_on_first_call_and_false_on_subsequent_calls()
      {
         var runOnce = new RunOnceAsync();
         runOnce.IsFirstCall().Must().BeTrue();
         runOnce.IsFirstCall().Must().BeFalse();
         runOnce.IsFirstCall().Must().BeFalse();
      }
   }
}
