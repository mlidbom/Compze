using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.ThreadingCE;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using Xunit;

namespace Compze.Utilities.Tests.SystemCE.ThreadingCE;

public class RunOnce_specification
{
   public class RunIfFirstCall
   {
      [XF] public void Executes_the_action_on_first_call()
      {
         var runOnce = new RunOnce();
         var executed = false;
         runOnce.RunIfFirstCall(() => executed = true);
         executed.Must().BeTrue();
      }

      [XF] public void Does_not_execute_the_action_on_second_call()
      {
         var runOnce = new RunOnce();
         var executionCount = 0;
         runOnce.RunIfFirstCall(() => Interlocked.Increment(ref executionCount));
         runOnce.RunIfFirstCall(() => Interlocked.Increment(ref executionCount));
         executionCount.Must().Be(1);
      }

      [XF] public void Concurrent_caller_waits_for_first_call_to_complete()
      {
         var runOnce = new RunOnce();
         using var actionStarted = new ManualResetEventSlim(false);
         using var allowActionToComplete = new ManualResetEventSlim(false);
         var events = new ConcurrentQueue<string>();

         var firstCall = Task.Run(() =>
         {
            runOnce.RunIfFirstCall(() =>
            {
               actionStarted.Set();
               allowActionToComplete.Wait();
               events.Enqueue("first_completed");
            });
         });

         actionStarted.Wait();

         var secondCall = Task.Run(() =>
         {
            runOnce.RunIfFirstCall(() => throw new InvalidOperationException("Should not execute"));
            events.Enqueue("second_returned");
         });

         allowActionToComplete.Set();
         firstCall.Wait();
         secondCall.Wait();

         events.ToArray().Must().SequenceEqual(["first_completed", "second_returned"]);
      }

      [XF] public void Concurrent_caller_receives_exception_from_first_call()
      {
         var runOnce = new RunOnce();
         using var actionStarted = new ManualResetEventSlim(false);

         var firstCall = Task.Run(() =>
         {
            Assert.Throws<InvalidOperationException>(() =>
               runOnce.RunIfFirstCall(() =>
               {
                  actionStarted.Set();
                  throw new InvalidOperationException("Schema creation failed");
               }));
         });

         actionStarted.Wait();

         var secondCall = Task.Run(() =>
         {
            var ex = Assert.Throws<AggregateException>(() =>
               runOnce.RunIfFirstCall(() => throw new InvalidOperationException("Should not execute")));
            ex.InnerExceptions[0].Message.Must().Contain("Schema creation failed");
         });

         firstCall.Wait();
         secondCall.Wait();
      }
   }

   public class IsFirstCall
   {
      [XF] public void Returns_true_on_first_call_and_false_on_subsequent_calls()
      {
         var runOnce = new RunOnce();
         runOnce.IsFirstCall().Must().BeTrue();
         runOnce.IsFirstCall().Must().BeFalse();
         runOnce.IsFirstCall().Must().BeFalse();
      }
   }
}
