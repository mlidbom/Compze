using System;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.DbPool.SystemCE.ThreadingCE;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Performance.Internals.SystemCE.ThreadingCE;

public class PersistentMachineWideSharedObjectPerformanceTests : UniversalTestBase
{
   readonly MachineWideSharedObject<SharedObject> _shared = MachineWideSharedObject<SharedObject>.For(Guid.NewGuid().ToString());

   protected override void DisposeInternal() => _shared.Delete();

   [XF] public void Get_copy_runs_single_threaded_XX_times_in_50_milliseconds()
      => TimeAsserter.Execute(() => _shared.GetCopy(), iterations: 100, maxTotal: 50.Milliseconds());

   [XF] public void Get_copy_runs_multi_threaded_XX_times_in_50_milliseconds() =>
      TimeAsserter.ExecuteThreaded(() => _shared.GetCopy(), iterations: 100, maxTotal: 50.Milliseconds());

   [XF] public void Update_runs_single_threaded_XX_times_in_50_milliseconds() =>
      TimeAsserter.Execute(() => _shared.Update(it => it.Name = ""), iterations: 30, maxTotal: 50.Milliseconds(), maxTries: 10);

   [XF] public void Update_runs_multi_threaded_60_times_in_80_milliseconds() =>
      TimeAsserter.ExecuteThreaded(() => _shared.Update(it => it.Name = ""), iterations: 60, maxTotal: 100.Milliseconds(), maxTries: 10);
}