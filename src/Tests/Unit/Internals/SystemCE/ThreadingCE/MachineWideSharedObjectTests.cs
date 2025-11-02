using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Compze.Core.Serialization.Internal.DbPool;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Threading.Testing;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Testing.DbPool.SystemCE.ThreadingCE;
using Compze.Tests.Infrastructure.Fluent;

using JetBrains.Annotations;
using Compze.Utilities.Threading.TasksCE;

// ReSharper disable ImplicitlyCapturedClosure

namespace Compze.Tests.Unit.Internals.SystemCE.ThreadingCE;

[UsedImplicitly] public class SharedObject
{
   public string Name { get; set; } = "Default";
}

public class MachineWideSharedObjectTests : UniversalTestBase
{
   readonly List<MachineWideSharedObject<SharedObject>> _created = new();
   readonly IServiceLocator _serviceLocator;
   readonly ISharedObjectSerializer _serializer;

   public MachineWideSharedObjectTests()
   {
      _serviceLocator = TestEnv.DIContainer.CreateWithServiceLocatorAndCurrentTestsPluggableComponents().ServiceLocator;
      _serializer = _serviceLocator.Resolve<ISharedObjectSerializer>();
   }

   protected override void DisposeInternal()
   {
      _created.ForEach(obj => obj.Delete());
      _serviceLocator.Dispose();
   }

   MachineWideSharedObject<SharedObject> CreateAndDeleteFileWhenTestCompletes(string name)
   {
      var created = MachineWideSharedObject<SharedObject>.For(name, _serializer);
      _created.Add(created);
      return created;
   }

   [PCT] public void Create()
   {
      var name = Guid.NewGuid().ToString();
      var shared = CreateAndDeleteFileWhenTestCompletes(name);
      var test = shared.GetCopy();

      test.Name.Must().Be("Default");
   }

   [PCT] public void Create_update_and_get()
   {
      var name = Guid.NewGuid().ToString();
      var shared = CreateAndDeleteFileWhenTestCompletes(name);
      var value = shared.GetCopy();

      value.Name.Must().Be("Default");

      value = shared.Update(it => it.Name = "Updated");

      value.Name.Must().Be("Updated");

      value = shared.GetCopy();

      value.Name.Must().Be("Updated");
   }

   [PCT] public void Two_instances_with_same_name_share_data()
   {
      var name = Guid.NewGuid().ToString();
      var shared1 = CreateAndDeleteFileWhenTestCompletes(name);
      var shared2 = CreateAndDeleteFileWhenTestCompletes(name);
      var test1 = shared1.GetCopy();
      var test2 = shared2.GetCopy();

      test1.Name.Must().Be("Default");
      test2.Name.Must().Be("Default");

      test1 = shared1.Update(it => it.Name = "Updated");
      test2 = shared2.GetCopy();

      test1.Name.Must().Be("Updated");
      test2.Name.Must().Be("Updated");

      test1 = shared1.GetCopy();
      test1.Name.Must().Be("Updated");
   }

   [PCT] public void Persistent_Once_all_instance_are_disposed_data_is_retained()
   {
      const string name = "40BD77DF-7C32-4B28-9A49-DA2CE202CC4F";
      var newName = Guid.NewGuid().ToString();
      MachineWideSharedObject<SharedObject> shared2;
      var shared = CreateAndDeleteFileWhenTestCompletes(name);

      shared.Update(it => it.Name = newName).Name.Must().Be(newName);
      shared2 = CreateAndDeleteFileWhenTestCompletes(name);
      shared.GetCopy().Name.Must().Be(newName);

      shared2.GetCopy().Name.Must().Be(newName);

      shared = CreateAndDeleteFileWhenTestCompletes(name);
      shared.GetCopy().Name.Must().Be(newName);
   }

   [PCT] public async Task Update_blocks_GetCopy_and_Update_from_both_same_and_other_instances()
   {
      var timeout = 15.Seconds();
      var updateGate = ThreadGate.CreateClosedWithTimeout(timeout);
      var conflictingUpdateSectionSameInstance = GatedCodeSection.WithTimeout(timeout);
      var conflictingUpdateSectionOtherInstance = GatedCodeSection.WithTimeout(timeout);
      var conflictingGetCopySectionSameInstance = GatedCodeSection.WithTimeout(timeout);
      var conflictingGetCopySectionOtherInstance = GatedCodeSection.WithTimeout(timeout);

      IList<IGatedCodeSection> conflictingSections =
      [
         conflictingUpdateSectionSameInstance,
         conflictingUpdateSectionOtherInstance,
         conflictingGetCopySectionSameInstance,
         conflictingGetCopySectionOtherInstance
      ];

      var name = Guid.NewGuid().ToString();
      var shared1 = CreateAndDeleteFileWhenTestCompletes(name);
      var shared2 = CreateAndDeleteFileWhenTestCompletes(name);
      // ReSharper disable AccessToDisposedClosure
      var tasks = Task.WhenAll(
         TaskCE.Run(() => shared1.Update(_ => { updateGate.AwaitPassThrough(); })),
         TaskCE.Run(() => conflictingUpdateSectionSameInstance.Execute(() => shared1.Update(_ => {}))),
         TaskCE.Run(() => conflictingGetCopySectionSameInstance.Execute(() => shared1.GetCopy())),
         TaskCE.Run(() => conflictingUpdateSectionOtherInstance.Execute(() => shared2.Update(_ => {}))),
         TaskCE.Run(() => conflictingGetCopySectionOtherInstance.Execute(() => shared2.GetCopy())));

      updateGate.AwaitQueueLengthEqualTo(1);
      conflictingSections.ForEach(section =>
      {
         section.EntranceGate.AwaitQueueLengthEqualTo(1);
         section.Open();
      });

      Thread.Sleep(50.Milliseconds());

      conflictingSections.ForEach(it => it.ExitGate.PassedThrough.Count.Must().Be(0));
      updateGate.Open();
      conflictingSections.ForEach(it => it.ExitGate.AwaitPassedThroughCountEqualTo(1));

      await tasks;
      // ReSharper restore AccessToDisposedClosure
   }
}
