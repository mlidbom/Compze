using System.Diagnostics;
using System.Transactions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.Threading.Testing.Specifications.Async;

[Collection(nameof(NonParallelCollection))]
public class A_thousand_async_passes_through_a_closed_ThreadGate : UniversalTestBase
{
   const int NumberOfPasses = 1000;
   readonly IThreadGate _gate = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "gate");
   readonly int _processThreadsBeforeThePasses;
   readonly int _processThreadsWhileTheyWait;
   readonly IReadOnlyList<Task> _passes;

   public A_thousand_async_passes_through_a_closed_ThreadGate()
   {
      _processThreadsBeforeThePasses = ProcessThreadCount();
      _passes = [..Enumerable.Range(0, NumberOfPasses).Select(_ => _gate.AwaitPassThroughAsync())];
      _processThreadsWhileTheyWait = ProcessThreadCount();
   }

   protected override async Task DisposeAsyncInternal()
   {
      _gate.Open();
      await Task.WhenAll(_passes).AwaitCompletionWithinAsync(TimeSpan.FromSeconds(10));
      await base.DisposeAsyncInternal();
   }

   [XF] public void do_not_hold_a_thread_each() => (_processThreadsWhileTheyWait - _processThreadsBeforeThePasses).Must().BeLessThan(NumberOfPasses / 10);

   static int ProcessThreadCount()
   {
      using var process = Process.GetCurrentProcess();
      return process.Threads.Count;
   }
}
