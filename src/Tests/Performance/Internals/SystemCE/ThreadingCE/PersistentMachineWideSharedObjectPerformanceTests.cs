using System;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.DbPool.SystemCE.ThreadingCE;

namespace Compze.Tests.Performance.Internals.SystemCE.ThreadingCE;

[Performance]
public class PersistentMachineWideSharedObjectPerformanceTests : UniversalTestBase
{
   public PersistentMachineWideSharedObjectPerformanceTests()
   {
      var name = Guid.NewGuid().ToString();
      _shared = MachineWideSharedObject<SharedObject>.For(name, usePersistentFile: true);
   }

   readonly MachineWideSharedObject<SharedObject> _shared;

   public void Dispose()
   {
      _shared.Dispose();
   }

   [XF] public void Get_copy_runs_single_threaded_100_times_in_40_milliseconds()
      => TimeAsserter.Execute(() => _shared.GetCopy(), iterations: 100, maxTotal: 40.Milliseconds());

   [XF] public void Get_copy_runs_multi_threaded_100_times_in_50_milliseconds() =>
      TimeAsserter.ExecuteThreaded(() => _shared.GetCopy(), iterations: 100, maxTotal: 50.Milliseconds());

   [XF] public void Update_runs_single_threaded_100_times_in_80_milliseconds() =>
      TimeAsserter.Execute(() => _shared.Update(it => it.Name = ""), iterations: 100, maxTotal: 80.Milliseconds(), maxTries: 10);

   [XF] public void Update_runs_multi_threaded_100_times_in_80_milliseconds() =>
      TimeAsserter.ExecuteThreaded(() => _shared.Update(it => it.Name = ""), iterations: 100, maxTotal: 80.Milliseconds(), maxTries: 10);
}