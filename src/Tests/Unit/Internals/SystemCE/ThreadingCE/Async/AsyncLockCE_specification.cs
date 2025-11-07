using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.Async;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.SystemCE.ThreadingCE.Testing;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

// ReSharper disable AccessToDisposedClosure

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Internals.SystemCE.ThreadingCE.Async;

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
         var firstTaskTookLockGate = ThreadGate.CreateClosedWithTimeout(1.Seconds());
         var secondTaskTookLockGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

         var firstTask = asyncLock.LockedAsync(async () =>
         {
            await Task.Yield();
            firstTaskTookLockGate.AwaitPassThrough();
         });

         firstTaskTookLockGate.AwaitQueueLengthEqualTo(1);

         var secondTask = asyncLock.LockedAsync(async () => secondTaskTookLockGate.AwaitPassThrough());

         secondTaskTookLockGate.TryAwaitPassedThroughCountEqualTo(1, 10.Milliseconds()).Must().Be(false);
         firstTaskTookLockGate.Open();
         secondTaskTookLockGate.TryAwaitPassedThroughCountEqualTo(1, 10.Milliseconds()).Must().Be(true);

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
         var task1TookLockGate = ThreadGate.CreateClosedWithTimeout(1.Seconds());
         var task2TookLockGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

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

         task2TookLockGate.TryAwaitPassedThroughCountEqualTo(1, 10.Milliseconds()).Must().Be(false);
         task1TookLockGate.Open();
         task2TookLockGate.TryAwaitPassedThroughCountEqualTo(1, 10.Milliseconds()).Must().Be(true);

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
         var task1TookLockGate = ThreadGate.CreateClosedWithTimeout(1.Seconds());
         var task2TookLockGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

         var task1 = TaskCE.Run(() => asyncLock.Locked(() => task1TookLockGate.AwaitPassThrough()));

         task1TookLockGate.AwaitQueueLengthEqualTo(1);

         var task2 = TaskCE.Run(() => asyncLock.Locked(() => task2TookLockGate.AwaitPassThrough()));

         task2TookLockGate.TryAwaitPassedThroughCountEqualTo(1, 10.Milliseconds()).Must().Be(false);
         task1TookLockGate.Open();
         task2TookLockGate.TryAwaitPassedThroughCountEqualTo(1, 10.Milliseconds()).Must().Be(true);

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

         var task = Task.Run(() => asyncLock.Locked(() => Task.Run(() => asyncLock.Locked(() => {}))));
      }

      [XF] public void it_allows_reentrant_calls_from_same_async_context_while_blocking_calls_from_same_thread()
      {
         using var asyncLock = IAsyncLockCE.WithDefaultTimeout();
         var firstTaskTookLockGate = ThreadGate.CreateClosedWithTimeout(1.Seconds());
         var firstTaskNestedTaskTookLockGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

         var secondTaskStartedGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
         var secondTaskGotLockGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

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
         secondTaskGotLockGate.TryAwaitPassedThroughCountEqualTo(1, 20.Milliseconds()).Must().BeFalse();
         firstTaskTookLockGate.Open();
         firstTaskNestedTaskTookLockGate.AwaitPassedThroughCountEqualTo(1);
         secondTaskGotLockGate.AwaitPassedThroughCountEqualTo(1, 20.Milliseconds());

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
         var task1TookLockGate = ThreadGate.CreateClosedWithTimeout(1.Seconds());
         var task2TookLockGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

         var task1 = TaskCE.Run(() => asyncLock.Locked(() => task1TookLockGate.AwaitPassThrough()));

         task1TookLockGate.AwaitQueueLengthEqualTo(1);

         var task2 = TaskCE.Run(() => asyncLock.Locked(() => task2TookLockGate.AwaitPassThrough()));

         task2TookLockGate.TryAwaitPassedThroughCountEqualTo(1, 10.Milliseconds()).Must().Be(false);
         task1TookLockGate.Open();
         task2TookLockGate.TryAwaitPassedThroughCountEqualTo(1, 10.Milliseconds()).Must().Be(true);

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
            asyncLock.Locked(() => { });
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
}
