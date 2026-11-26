using System.Diagnostics;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Interprocess;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.IProcessLock_;

///<summary>Another holder in this process: an <see cref="IProcessLock"/> of the same name in the same directory, as another process would make it.<br/>
/// What only another process can do, die holding the lock, is in <see cref="When_another_process_holds_the_lock"/>.</summary>
public class When_another_holder_in_this_process_holds_the_lock : ProcessLockSpecification
{
   const string TheHoldersPurpose = "a specification of what those who wait for the lock are told";

   readonly IProcessLock _theHoldersLock;
   readonly IProcessLockHold _theHoldersHold;

   public When_another_holder_in_this_process_holds_the_lock()
   {
      _theHoldersLock = AnotherInstanceOfTheLock();
      _theHoldersHold = Holding(_theHoldersLock.Take(TheHoldersPurpose, WaitTimeout.Zero))!;
   }

   [XF] public void TryTake_with_a_WaitTimeout_of_zero_returns_null() => Holding(Lock.TryTake(Purpose, WaitTimeout.Zero)).Must().BeNull();
   [XF] public async Task TryTakeAsync_with_a_WaitTimeout_of_zero_returns_null() => Holding(await Lock.TryTakeAsync(Purpose, WaitTimeout.Zero)).Must().BeNull();
   [XF] public void TryTake_on_the_instance_that_holds_it_returns_null() => Holding(_theHoldersLock.TryTake(Purpose, WaitTimeout.Zero)).Must().BeNull();

   [XF] public void LastHoldersDescription_says_what_the_holder_holds_it_for_which_process_it_is_on_which_machine_and_since_when() =>
      Lock.LastHoldersDescription().Must().StartWith($"for {TheHoldersPurpose}, by process {Environment.ProcessId} (")
          .Contain($") on {Environment.MachineName}, since ");

   public class and_TryTake_waits_200_milliseconds : When_another_holder_in_this_process_holds_the_lock
   {
      readonly IProcessLockHold? _hold;
      readonly TimeSpan _waited;

      public and_TryTake_waits_200_milliseconds()
      {
         var waiting = Stopwatch.StartNew();
         _hold = Holding(Lock.TryTake(Purpose, WaitTimeout.Milliseconds(200)));
         _waited = waiting.Elapsed;
      }

      [XF] public void it_returns_null() => _hold.Must().BeNull();
      [XF] public void it_returns_once_the_200_milliseconds_have_passed() => _waited.Must().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(200));
   }

   public class and_TryTakeAsync_waits_200_milliseconds : When_another_holder_in_this_process_holds_the_lock
   {
      IProcessLockHold? _hold;
      TimeSpan _waited;

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         var waiting = Stopwatch.StartNew();
         _hold = Holding(await Lock.TryTakeAsync(Purpose, WaitTimeout.Milliseconds(200)));
         _waited = waiting.Elapsed;
      }

      [XF] public void it_returns_null() => _hold.Must().BeNull();
      [XF] public void it_returns_once_the_200_milliseconds_have_passed() => _waited.Must().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(200));
   }

   public class and_Take_gives_up : When_another_holder_in_this_process_holds_the_lock
   {
      const string TheTakersPurpose = "a specification of what a Take that gives up says";
      readonly TimeoutException _exception;

      public and_Take_gives_up() =>
         _exception = Invoking(() => Holding(Lock.Take(TheTakersPurpose, WaitTimeout.Milliseconds(100)))).Must().Throw<TimeoutException>().Which;

      [XF] public void its_message_says_what_the_lock_is_held_for() => _exception.Message.Must().Contain(TheHoldersPurpose);
      [XF] public void its_message_names_the_process_that_holds_it() => _exception.Message.Must().Contain($"process {Environment.ProcessId} (");
      [XF] public void its_message_says_what_the_take_was_for() => _exception.Message.Must().Contain(TheTakersPurpose);
   }

   public class and_TakeAsync_gives_up : When_another_holder_in_this_process_holds_the_lock
   {
      [XF] public async Task it_throws_a_TimeoutException_saying_what_the_lock_is_held_for() =>
         (await InvokingAsync(async () => Holding(await Lock.TakeAsync(Purpose, WaitTimeout.Milliseconds(100)))).Must().ThrowAsync<TimeoutException>())
        .Which.Message.Must().Contain(TheHoldersPurpose);
   }

   public class and_the_hold_is_disposed_on_another_thread : When_another_holder_in_this_process_holds_the_lock
   {
      public and_the_hold_is_disposed_on_another_thread()
      {
         var otherThread = new Thread(_theHoldersHold.Dispose) { IsBackground = true, Name = nameof(and_the_hold_is_disposed_on_another_thread) };
         otherThread.Start();
         otherThread.Join();
      }

      [XF] public void the_lock_can_be_taken() => Holding(Lock.TryTake(Purpose, WaitTimeout.Zero)).Must().NotBeNull();
   }

   public class and_the_hold_is_disposed_while_TryTakeAsync_waits : When_another_holder_in_this_process_holds_the_lock
   {
      readonly Task<IProcessLockHold?> _wait;

      ///<remarks>TryTakeAsync tries once before it first awaits, so the wait has found the lock held by the time it returns.</remarks>
      public and_the_hold_is_disposed_while_TryTakeAsync_waits()
      {
         _wait = Lock.TryTakeAsync(Purpose, WaitTimeout.Seconds(30));
         _theHoldersHold.Dispose();
      }

      [XF] public async Task the_wait_returns_a_hold() => Holding(await _wait.WaitAsync(Patience)).Must().NotBeNull();
   }

   public class and_the_hold_is_disposed_while_TryTake_waits_on_another_thread : When_another_holder_in_this_process_holds_the_lock
   {
      readonly Task<IProcessLockHold?> _wait;

      public and_the_hold_is_disposed_while_TryTake_waits_on_another_thread()
      {
         _wait = Task.Factory.StartNew(() => Lock.TryTake(Purpose, WaitTimeout.Seconds(30)), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
         Thread.Sleep(200);
         _theHoldersHold.Dispose();
      }

      [XF] public async Task the_wait_returns_a_hold() => Holding(await _wait.WaitAsync(Patience)).Must().NotBeNull();
   }

   public class and_a_TryTakeAsync_that_waits_is_cancelled : When_another_holder_in_this_process_holds_the_lock
   {
      readonly CancellationTokenSource _cancellation = new();
      readonly Task<IProcessLockHold?> _wait;

      public and_a_TryTakeAsync_that_waits_is_cancelled()
      {
         _wait = Lock.TryTakeAsync(Purpose, WaitTimeout.Infinite, _cancellation.Token);
         _cancellation.Cancel();
      }

      protected override void DisposeInternal()
      {
         _cancellation.Dispose();
         base.DisposeInternal();
      }

      [XF] public async Task the_wait_throws_an_OperationCanceledException() =>
         await InvokingAsync(async () => Holding(await _wait.WaitAsync(Patience))).Must().ThrowAsync<OperationCanceledException>();
   }

   public class and_a_TryTake_that_waits_on_another_thread_is_cancelled : When_another_holder_in_this_process_holds_the_lock
   {
      readonly CancellationTokenSource _cancellation = new();
      readonly Task<IProcessLockHold?> _wait;

      public and_a_TryTake_that_waits_on_another_thread_is_cancelled()
      {
         _wait = Task.Factory.StartNew(() => Lock.TryTake(Purpose, WaitTimeout.Infinite, _cancellation.Token), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
         Thread.Sleep(200);
         _cancellation.Cancel();
      }

      protected override void DisposeInternal()
      {
         _cancellation.Dispose();
         base.DisposeInternal();
      }

      [XF] public async Task the_wait_throws_an_OperationCanceledException() =>
         await InvokingAsync(async () => Holding(await _wait.WaitAsync(Patience))).Must().ThrowAsync<OperationCanceledException>();
   }

   ///<remarks>Not in parallel with other specifications, whose threads would count as the waits'.</remarks>
   [Collection(nameof(NonParallelCollection))]
   public class and_far_more_TryTakeAsyncs_wait_than_the_thread_pool_has_threads : When_another_holder_in_this_process_holds_the_lock
   {
      const int NumberOfWaits = 200;
      readonly CancellationTokenSource _endOfTheWaits = new();
      readonly int _processThreadsBeforeTheWaits;
      readonly int _processThreadsWhileTheyWait;
      readonly IReadOnlyList<Task<IProcessLockHold?>> _waits;

      public and_far_more_TryTakeAsyncs_wait_than_the_thread_pool_has_threads()
      {
         _processThreadsBeforeTheWaits = ProcessThreadCount();
         _waits = [..Enumerable.Range(0, NumberOfWaits).Select(_ => Lock.TryTakeAsync(Purpose, WaitTimeout.Infinite, _endOfTheWaits.Token))];
         Thread.Sleep(500);
         _processThreadsWhileTheyWait = ProcessThreadCount();
      }

      protected override async Task DisposeAsyncInternal()
      {
         await _endOfTheWaits.CancelAsync();
         await Task.WhenAll(_waits).AwaitCompletionWithinAsync(Patience);
         _endOfTheWaits.Dispose();
         await base.DisposeAsyncInternal();
      }

      ///<remarks>Half the waits, not a tenth as for the monitor's waits, which do nothing until an update wakes them. These try to take the lock<br/>
      /// every 50 milliseconds on a thread-pool thread, all at once since they began together, and the pool adds threads for those bursts: 30 in<br/>
      /// this specification alone, measured 2026-11-26, and up to 48 beside the rest of the assembly. A thread held by each wait would add 200.</remarks>
      [XF] public void the_waits_do_not_hold_a_thread_each() =>
         (_processThreadsWhileTheyWait - _processThreadsBeforeTheWaits).Must().BeLessThan(NumberOfWaits / 2);

      static int ProcessThreadCount()
      {
         using var process = Process.GetCurrentProcess();
         return process.Threads.Count;
      }
   }
}
