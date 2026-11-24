using BenchmarkDotNet.Attributes;
using Compze.Threading;

namespace Compze.Tests.Benchmarks.Threading;

///<summary>What an <see cref="IAwaitableMonitor"/> costs: taking and releasing its locks, and creating one.</summary>
///<remarks>Taking and releasing a lock is on the hot path of nearly everything in Compze that is shared between threads,<br/>
/// and code may create a monitor for each of a great many objects.</remarks>
[MemoryDiagnoser]
public class MonitorBenchmarks
{
   readonly IAwaitableMonitor _monitor = IAwaitableMonitor.New();

   [Benchmark] public void Taking_and_releasing_a_read_lock()
   {
      using(_monitor.TakeReadLock()) {}
   }

   [Benchmark] public void Taking_and_releasing_an_update_lock()
   {
      using(_monitor.TakeUpdateLock()) {}
   }

   [Benchmark] public void Taking_a_read_lock_within_a_read_lock_and_releasing_both()
   {
      using(_monitor.TakeReadLock())
      using(_monitor.TakeReadLock()) {}
   }

   [Benchmark] public IAwaitableMonitor Creating_a_monitor() => IAwaitableMonitor.New();
}
