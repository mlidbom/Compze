using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.Threading.Testing.Specifications.Async;

[Collection(nameof(NonParallelCollection))]
public class TestingTaskRunner_ : UniversalTestBase
{
   readonly TestingTaskRunner _runner = TestingTaskRunner.WithTimeout(TimeSpan.FromSeconds(10));

   public class running_an_action : TestingTaskRunner_
   {
      bool _ranOnAThreadPoolThread = true;

      public running_an_action() => _runner.Run(() => { _ranOnAThreadPoolThread = Thread.CurrentThread.IsThreadPoolThread; });

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await _runner.DisposeAsync();
      }

      [XF] public void runs_it_on_a_thread_outside_the_thread_pool() => _ranOnAThreadPoolThread.Must().BeFalse();
   }

   public class running_an_async_function : TestingTaskRunner_
   {
      bool _ranToItsEnd;

      public running_an_async_function() => _runner.Run(async () =>
      {
         await Task.Yield();
         _ranToItsEnd = true;
      });

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await _runner.DisposeAsync();
      }

      [XF] public void disposal_awaits_it_to_its_end() => _ranToItsEnd.Must().BeTrue();
   }

   public class running_an_async_function_that_throws_after_awaiting : TestingTaskRunner_
   {
      public running_an_async_function_that_throws_after_awaiting() => _runner.Run(async () =>
      {
         await Task.Yield();
         throw new IntentionalSpecificationException();
      });

      [XF] public async Task disposal_fails_with_the_exception_it_threw() =>
         await InvokingAsync(async () => await _runner.DisposeAsync()).Must().ThrowAsync<IntentionalSpecificationException>();
   }
}
