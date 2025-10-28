using System;
using System.Collections.Generic;
using Compze.Core.Serialization.Internal.DbPool;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.DbPool.SystemCE.ThreadingCE;

namespace Compze.Tests.Performance.Internals.SystemCE.ThreadingCE;

public class MachineWideSharedObjectPerformanceTests : UniversalTestBase
{
   readonly List<MachineWideSharedObject<SharedObject>> _created = new();

   readonly IServiceLocator _serviceLocator;
   readonly ISharedObjectSerializer _serializer;

   public MachineWideSharedObjectPerformanceTests()
   {
      _serviceLocator = TestEnv.DIContainer.CreateWithServiceLocatorAndCurrentTestsPluggableComponents().ServiceLocator;
      _serializer = _serviceLocator.Resolve<ISharedObjectSerializer>();
   }

   protected override void DisposeInternal()
   {
      _serviceLocator.Dispose();
      _created.ForEach(obj => obj.Delete());
   }

   MachineWideSharedObject<SharedObject> CreateAndDeleteFileWhenTestCompletes(string name)
   {
      var created = MachineWideSharedObject<SharedObject>.For(name, _serializer);
      _created.Add(created);
      return created;
   }


   [PCTSerializer] public void Get_copy_runs_single_threaded_XX_times_in_50_milliseconds()
   {
      var name = Guid.NewGuid().ToString();
      var shared = CreateAndDeleteFileWhenTestCompletes(name);
      var shared2 = CreateAndDeleteFileWhenTestCompletes(name);
      TimeAsserter.Execute(() => shared.GetCopy(), iterations: 100, maxTotal: 50.Milliseconds());
      TimeAsserter.Execute(() => shared2.GetCopy(), iterations: 100, maxTotal: 50.Milliseconds());
   }

   [PCTSerializer] public void Get_copy_runs_multi_threaded_80_times_in_50_milliseconds()
   {
      var name = Guid.NewGuid().ToString();
      var shared = CreateAndDeleteFileWhenTestCompletes(name);
      var shared2 = CreateAndDeleteFileWhenTestCompletes(name);
      TimeAsserter.ExecuteThreaded(() => shared.GetCopy(), iterations: 80, maxTotal: 50.Milliseconds());
      TimeAsserter.ExecuteThreaded(() => shared2.GetCopy(), iterations: 80, maxTotal: 50.Milliseconds());
   }

   [PCTSerializer] public void Update_runs_single_threaded_XX_times_in_50_milliseconds()
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

   [PCTSerializer] public void Update_runs_multi_threaded_XX_times_in_50_milliseconds()
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