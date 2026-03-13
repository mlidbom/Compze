using Compze.InterprocessObject;
using Compze.Internals.Testing.Performance;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;

namespace Compze.DbPool.Tests.MachineWideState;

public class MachineWideSharedObjectPerformanceTests : UniversalTestBase
{
   readonly IInterprocessObject<SharedObject> _shared = IInterprocessObject.NewGlobalFileBacked(Guid.NewGuid().ToString(), new SharedObjectSerializer(), () => new SharedObject(), CorruptionAction.ThrowException);

   protected override void DisposeInternal() => _shared.Delete();

   [XF] public void Get_copy_runs_single_threaded_XX_times_in_50_milliseconds()
      => TimeAsserter.Execute(() => _shared.Read(_ => {}), iterations: 60, maxTotal: 50.Milliseconds());

   [XF] public void Get_copy_runs_multi_threaded_XX_times_in_50_milliseconds() =>
      TimeAsserter.ExecuteThreaded(() => _shared.Read(_ => {}), iterations: 75, maxTotal: 50.Milliseconds());

   [XF] public void Update_runs_single_threaded_XX_times_in_50_milliseconds() =>
      TimeAsserter.Execute(() => _shared.Update(it => it.Name = ""), iterations: 16, maxTotal: 50.Milliseconds(), maxTries: 10);

   [XF] public void Update_runs_multi_threaded_15_times_in_50_milliseconds() =>
      TimeAsserter.ExecuteThreaded(() => _shared.Update(it => it.Name = ""), iterations: 15, maxTotal: 50.Milliseconds(), maxTries: 10);
}
