using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using Newtonsoft.Json;
using System;
using Compze.Contracts;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Performance.Internals.SystemCE.ThreadingCE;

class SharedObjectSerializer : ISharedObjectSerializer<SharedObject>
{
   public string Serialize(SharedObject instance) => JsonConvert.SerializeObject(instance);

   public SharedObject Deserialize(string json) => JsonConvert.DeserializeObject<SharedObject>(json)._assert().NotNull();
}

public class MachineWideSharedObjectPerformanceTests : UniversalTestBase
{
   readonly MachineWideSharedObject<SharedObject> _shared = MachineWideSharedObject<SharedObject>.For(Guid.NewGuid().ToString(), new SharedObjectSerializer(), CorruptionAction.ThrowException);

   protected override void DisposeInternal() => _shared.Delete();

   [XF] public void Get_copy_runs_single_threaded_XX_times_in_50_milliseconds()
      => TimeAsserter.Execute(() => _shared.GetCopy(), iterations: 100, maxTotal: 50.Milliseconds());

   [XF] public void Get_copy_runs_multi_threaded_XX_times_in_50_milliseconds() =>
      TimeAsserter.ExecuteThreaded(() => _shared.GetCopy(), iterations: 75, maxTotal: 50.Milliseconds());

   [XF] public void Update_runs_single_threaded_XX_times_in_50_milliseconds() =>
      TimeAsserter.Execute(() => _shared.Update(it => it.Name = ""), iterations: 16, maxTotal: 50.Milliseconds(), maxTries: 10);

   [XF] public void Update_runs_multi_threaded_15_times_in_50_milliseconds() =>
      TimeAsserter.ExecuteThreaded(() => _shared.Update(it => it.Name = ""), iterations: 15, maxTotal: 50.Milliseconds(), maxTries: 10);
}
