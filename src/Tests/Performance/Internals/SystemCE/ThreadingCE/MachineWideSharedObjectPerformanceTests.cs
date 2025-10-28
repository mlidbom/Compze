using System;
using System.Collections.Generic;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.DbPool.SystemCE.ThreadingCE;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Performance.Internals.SystemCE.ThreadingCE;

public class MachineWideSharedObjectPerformanceTests : UniversalTestBase
{
   readonly List<MachineWideSharedObject<SharedObject>> _created = new();
   MachineWideSharedObject<SharedObject> CreateAndDeleteFileWhenTestCompletes(string name)
   {
      var created = MachineWideSharedObject<SharedObject>.For(name);
      _created.Add(created);
      return created;
   }

   protected override void DisposeInternal() => _created.ForEach(MachineWideSharedObject<SharedObject>.Delete);

   [XF] public void Get_copy_runs_single_threaded_XX_times_in_50_milliseconds()
   {
      var name = Guid.NewGuid().ToString();
      var shared = CreateAndDeleteFileWhenTestCompletes(name);
      var shared2 = CreateAndDeleteFileWhenTestCompletes(name);
      TimeAsserter.Execute(() => shared.GetCopy(), iterations: 100, maxTotal: 50.Milliseconds());
      TimeAsserter.Execute(() => shared2.GetCopy(), iterations: 100, maxTotal: 50.Milliseconds());
   }

   [XF] public void Get_copy_runs_multi_threaded_80_times_in_50_milliseconds()
   {
      var name = Guid.NewGuid().ToString();
      var shared = CreateAndDeleteFileWhenTestCompletes(name);
      var shared2 = CreateAndDeleteFileWhenTestCompletes(name);
      TimeAsserter.ExecuteThreaded(() => shared.GetCopy(), iterations: 80, maxTotal: 50.Milliseconds());
      TimeAsserter.ExecuteThreaded(() => shared2.GetCopy(), iterations: 80, maxTotal: 50.Milliseconds());
   }

   [XF] public void Update_runs_single_threaded_XX_times_in_50_milliseconds()
   {
      MachineWideSharedObject<SharedObject> shared = null!;
      var counter = 0;

      TimeAsserter.Execute(
         setup: () =>
         {
            counter = 0;
            shared = CreateAndDeleteFileWhenTestCompletes(Guid.NewGuid().ToString());
         },
         action: () => shared.Update(it => it.Name = (++counter).ToStringInvariant()),
         iterations: 40,
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
            shared = CreateAndDeleteFileWhenTestCompletes(Guid.NewGuid().ToString());
         },
         action: () => shared.Update(it => it.Name = (++counter).ToStringInvariant()),
         iterations: 25,
         maxTotal: 50.Milliseconds());
   }
}