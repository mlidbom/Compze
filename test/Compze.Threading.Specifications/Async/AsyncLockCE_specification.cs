using System.Runtime.CompilerServices;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Tests.Infrastructure;
using Compze.Internals.SystemCE;
using Compze.Threading.Testing;
using Compze.Internals.SystemCE.ThreadingCE.Async;
using Compze.Must;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable AccessToDisposedClosure

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.Async;

[Collection(nameof(NonParallelCollection))]
public class AsyncLockCE_specification : UniversalTestBase
{
   public class When_calling_LockedAsync_with_Func_Task : AsyncLockCE_specification
   {
      [XF] public async Task it_executes_the_action()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();
         var executed = false;
         await asyncLock.LockedAsync(async () =>
         {
            await Task.Yield();
            executed = true;
         });
         executed.Must().BeTrue();
      }

      [XF] public async Task it_blocks_concurrent_calls()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();
         var firstTaskTookLockGate = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "firstTaskTookLock");
         var secondTaskTookLockGate = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "secondTaskTookLock");

         var firstTask = asyncLock.LockedAsync(async () =>
         {
            await Task.Yield();
            firstTaskTookLockGate.AwaitPassThrough();
         });

         firstTaskTookLockGate.AwaitQueueLengthEqualTo(1);

         var secondTask = asyncLock.LockedAsync(async () => await Task.FromResult(secondTaskTookLockGate.AwaitPassThrough()));

         secondTaskTookLockGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(10)).Must().Be(false);
         firstTaskTookLockGate.Open();
         secondTaskTookLockGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(5)).Must().Be(true);

         await Task.WhenAll(firstTask, secondTask);
      }

      [XF] public async Task it_allows_reentrant_calls_from_same_async_context()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();

         await asyncLock.LockedAsync(async () =>
         {
            await asyncLock.LockedAsync(async () => await Task.Yield());
         });
      }
   }

   public class When_calling_LockedAsync_with_Func_Task_TReturn : AsyncLockCE_specification
   {
      [XF] public async Task it_returns_the_result()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();
         (await asyncLock.LockedAsync(async () =>
               {
                  await Task.Yield();
                  return 42;
               })).Must().Be(42);
      }

      [XF] public async Task it_blocks_concurrent_calls()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();
         var task1TookLockGate = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "task1TookLockGate");
         var task2TookLockGate = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "task2TookLockGate");

         var task1 = asyncLock.LockedAsync(async () =>
         {
            await Task.Yield();
            task1TookLockGate.AwaitPassThrough();
         });

         task1TookLockGate.AwaitQueueLengthEqualTo(1);

         var task2 = asyncLock.LockedAsync(async () =>
         {
            task2TookLockGate.AwaitPassThrough();
            await Task.Yield();
         });

         task2TookLockGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(10)).Must().Be(false);
         task1TookLockGate.Open();
         task2TookLockGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(5)).Must().Be(true);

         await Task.WhenAll(task1, task2);
      }

      [XF] public async Task it_allows_reentrant_calls_from_same_async_context()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();

         await asyncLock.LockedAsync(async () =>
                                        await asyncLock.LockedAsync(async () => await Task.Yield())); //Not hanging is success
      }
   }

   public class When_calling_Locked_with_Action : AsyncLockCE_specification
   {
      [XF] public void it_executes_the_action()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();
         var executed = false;
         asyncLock.Locked(() => executed = true);
         executed.Must().BeTrue();
      }

      [XF] public async Task it_blocks_concurrent_calls_from_different_threads()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();
         var task1TookLockGate = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "task1TookLock");
         var task2TookLockGate = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "task2TookLock");

         var task1 = TaskCE.Run(() => asyncLock.Locked(() => task1TookLockGate.AwaitPassThrough()));

         task1TookLockGate.AwaitQueueLengthEqualTo(1);

         var task2 = TaskCE.Run(() => asyncLock.Locked(() => task2TookLockGate.AwaitPassThrough()));

         task2TookLockGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(10)).Must().Be(false);
         task1TookLockGate.Open();
         task2TookLockGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(5)).Must().Be(true);

         await Task.WhenAll(task1, task2);
      }

      [XF] public void it_allows_reentrant_calls_from_same_thread()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();

         asyncLock.Locked(() => asyncLock.Locked(() => {}));
      }

      [XF] public void it_allows_reentrant_calls_from_same_async_context()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();

         Task.Run(() => asyncLock.Locked(() => Task.Run(() => asyncLock.Locked(() => {})))).Wait();
      }

      [XF] public void it_allows_reentrant_calls_from_same_async_context_while_blocking_calls_from_same_thread()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();
         var firstTaskTookLockGate = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "firstTaskTookLock");
         var firstTaskNestedTaskTookLockGate = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "firstTaskNestedTaskTookLock");

         var secondTaskStartedGate = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "secondTaskStarted");
         var secondTaskGotLockGate = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "secondTaskGotLock");
         var firstTask = Task.Run(() => asyncLock.Locked(() =>
         {
            firstTaskTookLockGate.AwaitPassThrough();
            return Task.Run(() => asyncLock.Locked(() => firstTaskNestedTaskTookLockGate.AwaitPassThrough()));
         }));

         firstTaskTookLockGate.AwaitQueueLengthEqualTo(1);

         var secondTask = Task.Run(() =>
         {
            secondTaskStartedGate.AwaitPassThrough();
            asyncLock.Locked(() => {});
            secondTaskGotLockGate.AwaitPassThrough();
         });

         secondTaskStartedGate.AwaitPassedThroughCountEqualTo(1);
         secondTaskGotLockGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(20)).Must().BeFalse();
         firstTaskTookLockGate.Open();
         firstTaskNestedTaskTookLockGate.AwaitPassedThroughCountEqualTo(1);
         secondTaskGotLockGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(5));

         Task.WaitAll(firstTask, secondTask);
      }
   }

   public class When_calling_Locked_with_Func_TReturn : AsyncLockCE_specification
   {
      [XF] public void it_returns_the_result()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();
         var result = asyncLock.Locked(() => 42);
         result.Must().Be(42);
      }

      [XF] public async Task it_blocks_concurrent_calls_from_different_threads()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();
         var task1TookLockGate = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "task1TookLock");
         var task2TookLockGate = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "task2TookLock");

         var task1 = TaskCE.Run(() => asyncLock.Locked(() => task1TookLockGate.AwaitPassThrough()));

         task1TookLockGate.AwaitQueueLengthEqualTo(1);

         var task2 = TaskCE.Run(() => asyncLock.Locked(() => task2TookLockGate.AwaitPassThrough()));

         task2TookLockGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(10)).Must().Be(false);
         task1TookLockGate.Open();
         task2TookLockGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(5)).Must().Be(true);

         await Task.WhenAll(task1, task2);
      }

      [XF] public void it_allows_reentrant_calls_from_same_thread()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();

         asyncLock.Locked(() => asyncLock.Locked(() => "inner")).Must().Be("inner");
      }
   }

   public class When_mixing_sync_and_async_calls : AsyncLockCE_specification
   {
      [XF] public async Task async_call_can_reenter_sync_call_from_same_context()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();

         await asyncLock.LockedAsync(async () => //not hanging is success
         {
            asyncLock.Locked(() => {});
            await Task.Yield();
         });
      }

      [XF] public async Task sync_call_can_reenter_async_call_from_same_context()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();

         asyncLock.Locked(() =>
         {
            asyncLock.LockedAsync(async () =>
            {
               await Task.Yield();
            }).Wait();
         });

         await Task.Yield();
      }
   }

   public class When_exception_is_thrown : AsyncLockCE_specification
   {
      [XF] public async Task LockedAsync_propagates_exception_and_releases_lock()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();

         await InvokingAsync(async () => await asyncLock.LockedAsync(async () =>
         {
            await Task.Yield();
            throw new InvalidOperationException("test");
         })).Must().ThrowAsync<InvalidOperationException>();

         await asyncLock.LockedAsync(async () => await Task.Yield());
      }

      [XF] public void Locked_propagates_exception_and_releases_lock()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();

         Invoking(() => asyncLock.Locked(() => throw new InvalidOperationException("test")))
           .Must()
           .Throw<InvalidOperationException>();

         asyncLock.Locked(() => {});
      }
   }

   public class After_disposing : AsyncLockCE_specification
   {
      [XF] public void Calling_locked_throws_ObjectDisposedException()
      {
         var asyncLock = IAsyncLockCE.WithDefaultTimeout();
         asyncLock.Dispose();

         Invoking(() => asyncLock.Locked(() => {}))
           .Must()
           .Throw<ObjectDisposedException>();
      }

      [XF] public async Task Calling_LockedAsync_throws_ObjectDisposedException()
      {
         var asyncLock = IAsyncLockCE.WithDefaultTimeout();
         asyncLock.Dispose();

         await InvokingAsync(async () => await asyncLock.LockedAsync(async () => await Task.Yield()))
              .Must()
              .ThrowAsync<ObjectDisposedException>();
      }
   }

   public class When_timeout_is_exceeded : AsyncLockCE_specification
   {
      [XF] public async Task LockedAsync_throws_AsyncLockTimeoutException()
      {
         using var asyncLock = IAsyncLockCE.New(LockTimeout.Milliseconds(50));
         var firstTaskTookLockGate = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "firstTaskTookLockGate");

         var firstTask = asyncLock.LockedAsync(async () =>
         {
            await Task.Yield();
            firstTaskTookLockGate.AwaitPassThrough();
         });

         firstTaskTookLockGate.AwaitQueueLengthEqualTo(1);

         await InvokingAsync(async () => await asyncLock.LockedAsync(async () => await Task.Yield()))
              .Must()
              .ThrowAsync<AsyncLockTimeoutException>();

         firstTaskTookLockGate.Open();
         await firstTask;
      }

      [XF] public async Task Locked_throws_AsyncLockTimeoutException()
      {
         using var asyncLock = IAsyncLockCE.New(LockTimeout.Milliseconds(50));
         var firstTaskTookLockGate = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "firstTaskTookLockGate");

         var firstTask = TaskCE.Run(() => asyncLock.Locked(() => firstTaskTookLockGate.AwaitPassThrough()));

         firstTaskTookLockGate.AwaitQueueLengthEqualTo(1);

         Invoking(() => asyncLock.Locked(() => {}))
           .Must()
           .Throw<AsyncLockTimeoutException>();

         firstTaskTookLockGate.Open();
         await firstTask;
      }

      [XF] public async Task timeout_does_not_affect_reentrant_calls()
      {
         using var asyncLock = IAsyncLockCE.New(LockTimeout.Milliseconds(50));

         await asyncLock.LockedAsync(async () =>
         {
            await Task.Delay(100.Milliseconds());
            await asyncLock.LockedAsync(async () => await Task.Yield()); // Should not timeout
         });
      }

      [XF] public async Task exception_message_includes_blocking_thread_stack_trace()
      {
         using var asyncLock = IAsyncLockCE.New(LockTimeout.Milliseconds(50));
         asyncLock.SetTimeToWaitForStackTrace(WaitTimeout.Seconds(.5));
         var firstTaskTookLockGate = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "firstTaskTookLockGate");

         var firstTask = TaskCE.Run(() => HoldLockInMethodSoItWillBeInTheCapturedCallStack(asyncLock, firstTaskTookLockGate));

         firstTaskTookLockGate.AwaitQueueLengthEqualTo(1);

         var caughtException = await InvokingAsync(async () => await asyncLock.LockedAsync(async () => await Task.Yield()))
                                    .Must()
                                    .ThrowAsync<AsyncLockTimeoutException>();

         var caughtException2 = await InvokingAsync(async () => await asyncLock.LockedAsync(async () => await Task.Yield()))
                                    .Must()
                                    .ThrowAsync<AsyncLockTimeoutException>();

         firstTaskTookLockGate.Open();
         await firstTask;

         // The stack trace should show our specific method name, proving we captured the blocking thread's user code stack

         Console.WriteLine(caughtException.Which.Message);

         caughtException.Which.Message.Must()
                        .Contain(nameof(HoldLockInMethodSoItWillBeInTheCapturedCallStack))
                        .Contain("Blocking thread lock disposal stack trace");

         caughtException2.Which.Message.Must()
                        .Contain(nameof(HoldLockInMethodSoItWillBeInTheCapturedCallStack))
                        .Contain("Blocking thread lock disposal stack trace");
      }

      [MethodImpl(MethodImplOptions.NoInlining)]
      static void HoldLockInMethodSoItWillBeInTheCapturedCallStack(IAsyncLockCE asyncLock, IThreadGate gate) => asyncLock.Locked(gate.AwaitPassThrough);
   }
}
