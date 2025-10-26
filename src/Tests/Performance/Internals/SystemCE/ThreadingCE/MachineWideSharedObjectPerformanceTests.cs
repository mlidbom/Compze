using System;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.DbPool.SystemCE.ThreadingCE;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Performance.Internals.SystemCE.ThreadingCE;

public class MachineWideSharedObjectPerformanceTests : UniversalTestBase
{
   [XF] public void Get_copy_runs_single_threaded_100_times_in_40_milliseconds()
   {
      var name = Guid.NewGuid().ToString();
      using var shared = MachineWideSharedObject<SharedObject>.For(name);
      using var shared2 = MachineWideSharedObject<SharedObject>.For(name);
      TimeAsserter.Execute(() => shared.GetCopy(), iterations: 100, maxTotal: 40.Milliseconds());
      TimeAsserter.Execute(() => shared2.GetCopy(), iterations: 100, maxTotal: 40.Milliseconds());
   }

   [XF] public void Get_copy_runs_multi_threaded_100_times_in_50_milliseconds()
   {
      var name = Guid.NewGuid().ToString();
      using var shared = MachineWideSharedObject<SharedObject>.For(name);
      using var shared2 = MachineWideSharedObject<SharedObject>.For(name);
      TimeAsserter.ExecuteThreaded(() => shared.GetCopy(), iterations: 100, maxTotal: 50.Milliseconds());
      TimeAsserter.ExecuteThreaded(() => shared2.GetCopy(), iterations: 100, maxTotal: 50.Milliseconds());
   }

   [XF] public void Update_runs_single_threaded_XX_times_in_50_milliseconds()
   {
      MachineWideSharedObject<SharedObject> shared = null!;
      var counter = 0;

      TimeAsserter.Execute(
         setup: () =>
         {
            counter = 0;
            shared = MachineWideSharedObject<SharedObject>.For(Guid.NewGuid().ToString());
         },
         tearDown: () => shared.Dispose(),
         action: () => shared.Update(it => it.Name = (++counter).ToStringInvariant()),
         iterations: 50,
         maxTotal: 50.Milliseconds());
   }

   [XF] public void Update_runs_multi_threaded_XX_times_in_50_milliseconds()
   {
      MachineWideSharedObject<SharedObject> shared = null!;
      var counter = 0;

      TimeAsserter.ExecuteThreaded(
         setup: () =>
         {
            counter = 0;
            shared = MachineWideSharedObject<SharedObject>.For(Guid.NewGuid().ToString());
         },
         tearDown: () => shared.Dispose(),
         action: () => shared.Update(it => it.Name = (++counter).ToStringInvariant()),
         iterations: 25,
         maxTotal: 50.Milliseconds());
   }
}