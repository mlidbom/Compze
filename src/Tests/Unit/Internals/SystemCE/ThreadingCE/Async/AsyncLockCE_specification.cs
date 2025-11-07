using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.Async;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Internals.SystemCE.ThreadingCE.Async;

public class AsyncLockCE_specification : UniversalTestBase
{
   public class When_calling_LockedAsync_with_Func_Task : AsyncLockCE_specification
   {
      [XF] public async Task it_executes_the_action()
      {
         using var asyncLock = new AsyncLockCE();
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
         using var asyncLock = new AsyncLockCE();
         var firstTaskStarted = false;
         var secondTaskStarted = false;
         var firstTaskCompleted = false;
         var secondTaskSeesFirstCompleted = false;

         var firstTask = asyncLock.LockedAsync(async () =>
         {
            firstTaskStarted = true;
            await Task.Delay(50.Milliseconds());
            firstTaskCompleted = true;
         });

         await Task.Delay(10.Milliseconds());

         var secondTask = asyncLock.LockedAsync(async () =>
         {
            secondTaskStarted = true;
            secondTaskSeesFirstCompleted = firstTaskCompleted;
            await Task.Yield();
         });

         await Task.WhenAll(firstTask, secondTask);

         firstTaskStarted.Must().BeTrue();
         secondTaskStarted.Must().BeTrue();
         firstTaskCompleted.Must().BeTrue();
         secondTaskSeesFirstCompleted.Must().BeTrue();
      }

      [XF] public async Task it_allows_reentrant_calls_from_same_async_context()
      {
         using var asyncLock = new AsyncLockCE();

         await asyncLock.LockedAsync(async () =>
         {
            await asyncLock.LockedAsync(async () =>
            {
               await Task.Yield();
            });
         });
      }
   }

   public class When_calling_LockedAsync_with_Func_Task_TReturn : AsyncLockCE_specification
   {
      [XF] public async Task it_returns_the_result()
      {
         using var asyncLock = new AsyncLockCE();
         (await asyncLock.LockedAsync(async () =>
               {
                  await Task.Yield();
                  return 42;
               })).Must().Be(42);
      }

      [XF] public async Task it_blocks_concurrent_calls()
      {
         using var asyncLock = new AsyncLockCE();
         var counter = 0;

         var task1 = asyncLock.LockedAsync(async () =>
         {
            await Task.Delay(50.Milliseconds());
            counter++;
            return counter;
         });

         await Task.Delay(10.Milliseconds());

         var task2 = asyncLock.LockedAsync(async () =>
         {
            counter++;
            await Task.Yield();
            return counter;
         });

         var results = await Task.WhenAll(task1, task2);
         results[0].Must().Be(1);
         results[1].Must().Be(2);
      }

      [XF] public async Task it_allows_reentrant_calls_from_same_async_context()
      {
         using var asyncLock = new AsyncLockCE();
         var result = await asyncLock.LockedAsync(async () =>
         {
            var innerResult = await asyncLock.LockedAsync(async () =>
            {
               await Task.Yield();
               return "inner";
            });
            return $"outer-{innerResult}";
         });

         result.Must().Be("outer-inner");
      }
   }

   public class When_calling_Locked_with_Action : AsyncLockCE_specification
   {
      [XF] public void it_executes_the_action()
      {
         using var asyncLock = new AsyncLockCE();
         var executed = false;
         asyncLock.Locked(() => executed = true);
         executed.Must().BeTrue();
      }

      [XF] public async Task it_blocks_concurrent_calls_from_different_threads()
      {
         using var asyncLock = new AsyncLockCE();
         var syncTaskStarted = false;
         var syncTaskCompleted = false;
         var otherTaskSeesCompleted = false;

         var syncTask = TaskCE.Run(() => asyncLock.Locked(() =>
         {
            syncTaskStarted = true;
            Thread.Sleep(50.Milliseconds());
            syncTaskCompleted = true;
         }));

         await Task.Delay(10.Milliseconds());

         var otherTask = TaskCE.Run(() => asyncLock.Locked(() =>
         {
            otherTaskSeesCompleted = syncTaskCompleted;
         }));

         await Task.WhenAll(syncTask, otherTask);

         syncTaskStarted.Must().BeTrue();
         syncTaskCompleted.Must().BeTrue();
         otherTaskSeesCompleted.Must().BeTrue();
      }

      [XF] public void it_allows_reentrant_calls_from_same_thread()
      {
         using var asyncLock = new AsyncLockCE();
         var outerExecuted = false;
         var innerExecuted = false;

         asyncLock.Locked(() =>
         {
            outerExecuted = true;
            asyncLock.Locked(() => innerExecuted = true);
         });

         outerExecuted.Must().BeTrue();
         innerExecuted.Must().BeTrue();
      }
   }

   public class When_calling_Locked_with_Func_TReturn : AsyncLockCE_specification
   {
      [XF] public void it_returns_the_result()
      {
         using var asyncLock = new AsyncLockCE();
         var result = asyncLock.Locked(() => 42);
         result.Must().Be(42);
      }

      [XF] public async Task it_blocks_concurrent_calls_from_different_threads()
      {
         using var asyncLock = new AsyncLockCE();
         var counter = 0;

         var task1 = TaskCE.Run(() => asyncLock.Locked(() =>
         {
            Thread.Sleep(50.Milliseconds());
            counter++;
            return counter;
         }));

         await Task.Delay(10.Milliseconds());

         var task2 = TaskCE.Run(() => asyncLock.Locked(() =>
         {
            counter++;
            return counter;
         }));

         var results = await Task.WhenAll(task1, task2);
         results[0].Must().Be(1);
         results[1].Must().Be(2);
      }

      [XF] public void it_allows_reentrant_calls_from_same_thread()
      {
         using var asyncLock = new AsyncLockCE();
         var result = asyncLock.Locked(() =>
         {
            var innerResult = asyncLock.Locked(() => "inner");
            return $"outer-{innerResult}";
         });

         result.Must().Be("outer-inner");
      }
   }

   public class When_mixing_sync_and_async_calls : AsyncLockCE_specification
   {
      [XF] public async Task async_call_can_reenter_sync_call_from_same_context()
      {
         using var asyncLock = new AsyncLockCE();
         var result = "";

         await asyncLock.LockedAsync(async () =>
         {
            result += "async-outer-";
            asyncLock.Locked(() => result += "sync-inner-");
            await Task.Yield();
            result += "async-outer-end";
         });

         result.Must().Be("async-outer-sync-inner-async-outer-end");
      }

      [XF] public async Task sync_call_can_reenter_async_call_from_same_context()
      {
         using var asyncLock = new AsyncLockCE();
         var result = "";

         asyncLock.Locked(() =>
         {
            result += "sync-outer-";
            asyncLock.LockedAsync(async () =>
            {
               result += "async-inner-";
               await Task.Yield();
               result += "async-inner-end-";
            }).Wait();
            result += "sync-outer-end";
         });

         await Task.Yield();
         result.Must().Be("sync-outer-async-inner-async-inner-end-sync-outer-end");
      }
   }

   public class When_exception_is_thrown : AsyncLockCE_specification
   {
      [XF] public async Task LockedAsync_propagates_exception_and_releases_lock()
      {
         using var asyncLock = new AsyncLockCE();

         await InvokingAsync(async () => await asyncLock.LockedAsync(async () =>
         {
            await Task.Yield();
            throw new InvalidOperationException("test");
         })).Must().ThrowAsync<InvalidOperationException>();

         var executed = false;
         await asyncLock.LockedAsync(async () =>
         {
            executed = true;
            await Task.Yield();
         });

         executed.Must().BeTrue();
      }

      [XF] public void Locked_propagates_exception_and_releases_lock()
      {
         using var asyncLock = new AsyncLockCE();

         Invoking(() => asyncLock.Locked(() => throw new InvalidOperationException("test")))
           .Must()
           .Throw<InvalidOperationException>();

         var executed = false;
         asyncLock.Locked(() => executed = true);
         executed.Must().BeTrue();
      }
   }

   public class When_disposing : AsyncLockCE_specification
   {
      [XF] public void Dispose_releases_underlying_semaphore()
      {
         var asyncLock = new AsyncLockCE();
         asyncLock.Dispose();

         Invoking(() => asyncLock.Locked(() => {}))
           .Must()
           .Throw<ObjectDisposedException>();
      }

      [XF] public async Task Dispose_after_async_operations_complete()
      {
         var asyncLock = new AsyncLockCE();
         await asyncLock.LockedAsync(async () => await Task.Yield());
         asyncLock.Dispose();

         await InvokingAsync(async () => await asyncLock.LockedAsync(async () => await Task.Yield()))
              .Must()
              .ThrowAsync<ObjectDisposedException>();
      }
   }
}
