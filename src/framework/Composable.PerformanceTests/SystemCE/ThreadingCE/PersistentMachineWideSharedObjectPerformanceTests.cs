using System;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using Composable.Testing.Performance;
using NUnit.Framework;

namespace Composable.Tests.SystemCE.ThreadingCE;

[TestFixture] public class PersistentMachineWideSharedObjectPerformanceTests
{
   MachineWideSharedObject<SharedObject> _shared;
   [SetUp] public void SetupTask()
   {
      var name = Guid.NewGuid().ToString();
      _shared = MachineWideSharedObject<SharedObject>.For(name, usePersistentFile: true);
   }

   [TearDown] public void TearDownTask() => _shared.Dispose();

   [Test] public void Get_copy_runs_single_threaded_100_times_in_40_milliseconds()
      => TimeAsserter.Execute(() => _shared.GetCopy(), iterations: 100, maxTotal: 40.Milliseconds());

   [Test] public void Get_copy_runs_multi_threaded_100_times_in_50_milliseconds() =>
      TimeAsserter.ExecuteThreaded(() => _shared.GetCopy(), iterations: 100, maxTotal: 50.Milliseconds());

   [Test] public void Update_runs_single_threaded_100_times_in_80_milliseconds() =>
      TimeAsserter.Execute(() => _shared.Update(it => it.Name = ""), iterations: 100, maxTotal: 80.Milliseconds(), maxTries: 10);

   [Test] public void Update_runs_multi_threaded_100_times_in_80_milliseconds() =>
      TimeAsserter.ExecuteThreaded(() => _shared.Update(it => it.Name = ""), iterations: 100, maxTotal: 80.Milliseconds(), maxTries: 10);
}