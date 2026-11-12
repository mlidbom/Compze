using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;
using Xunit;

namespace Compze.Threading.Testing.Specifications.Async;

[Collection(nameof(NonParallelCollection))]
public class An_async_execution_of_a_closed_GatedCodeSection : UniversalTestBase
{
   static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

   readonly IGatedCodeSection _section = IGatedCodeSection.NewClosed(WaitTimeout.Seconds(30), "section");
   bool _theCodeRan;
   readonly Task _execution;

   public An_async_execution_of_a_closed_GatedCodeSection() => _execution = _section.ExecuteAsync(async () =>
   {
      _theCodeRan = true;
      await Task.Yield();
   });

   protected override async Task DisposeAsyncInternal()
   {
      _section.Open();
      await _execution.AwaitCompletionWithinAsync(Patience);
      await base.DisposeAsyncInternal();
   }

   [XF] public void is_queued_at_the_entrance_gate() => _section.EntranceGate.Queued.Must().Be(1);

   public class after_LetOneThreadEnterAndReachExitAsync : An_async_execution_of_a_closed_GatedCodeSection
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await _section.LetOneThreadEnterAndReachExitAsync().WaitAsync(Patience);
      }

      [XF] public void the_code_has_run() => _theCodeRan.Must().BeTrue();
      [XF] public void it_is_held_at_the_exit_gate() => _section.ExitGate.Queued.Must().Be(1);
      [XF] public void the_execution_has_not_completed() => _execution.IsCompleted.Must().BeFalse();
   }

   public class after_LetOneThreadPassAsync : An_async_execution_of_a_closed_GatedCodeSection
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await _section.LetOneThreadPassAsync().WaitAsync(Patience);
         await _execution.WaitAsync(Patience);
      }

      [XF] public void the_execution_has_completed() => _execution.IsCompletedSuccessfully.Must().BeTrue();
   }
}
