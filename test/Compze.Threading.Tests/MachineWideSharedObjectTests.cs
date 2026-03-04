using Compze.Contracts;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Threading.ResourceAccess;
using Compze.Threading.Testing;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using Compze.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using JetBrains.Annotations;
using Newtonsoft.Json;

// ReSharper disable ImplicitlyCapturedClosure

namespace Compze.Threading.Tests;

[UsedImplicitly] public class SharedObject
{
   // ReSharper disable once MemberCanBeInternal
   public string Name { get; set; } = "Default";
}

class SharedObjectSerializer : ISharedObjectSerializer<SharedObject>
{
   public string Serialize(SharedObject instance) => JsonConvert.SerializeObject(instance);

   public SharedObject Deserialize(string json) => JsonConvert.DeserializeObject<SharedObject>(json)._assert().NotNull();
}

public class MachineWideSharedObjectTests : UniversalTestBase
{
   readonly List<MachineWideSharedObject<SharedObject>> _created = [];

   protected override void DisposeInternal() => _created.ForEach(obj => obj.Delete());

   MachineWideSharedObject<SharedObject> CreateAndDeleteFileWhenTestCompletes(string name)
   {
      var created = MachineWideSharedObject<SharedObject>.For(name, new SharedObjectSerializer(), CorruptionAction.ThrowException);
      _created.Add(created);
      return created;
   }

   [XF] public void Create()
   {
      var name = Guid.NewGuid().ToString();
      var shared = CreateAndDeleteFileWhenTestCompletes(name);
      var test = shared.GetCopy();

      test.Name.Must().Be("Default");
   }

   [XF] public void Create_update_and_get()
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

   [XF] public void Two_instances_with_same_name_share_data()
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

   [XF] public void Persistent_Once_all_instance_are_disposed_data_is_retained()
   {
      const string name = "40BD77DF-7C32-4B28-9A49-DA2CE202CC4F";
      var newName = Guid.NewGuid().ToString();
      var shared = CreateAndDeleteFileWhenTestCompletes(name);

      shared.Update(it => it.Name = newName).Name.Must().Be(newName);
      var shared2 = CreateAndDeleteFileWhenTestCompletes(name);
      shared.GetCopy().Name.Must().Be(newName);

      shared2.GetCopy().Name.Must().Be(newName);

      shared = CreateAndDeleteFileWhenTestCompletes(name);
      shared.GetCopy().Name.Must().Be(newName);
   }

   [XF] public async Task Update_blocks_GetCopy_and_Update_from_both_same_and_other_instances()
   {
      var timeout = WaitTimeout.Seconds(15);
      var updateGate = ThreadGate.Closed(timeout, "updateGate");
      var conflictingUpdateSectionSameInstance = GatedCodeSection.Closed(timeout, "conflictingUpdateSectionSameInstance");
      var conflictingUpdateSectionOtherInstance = GatedCodeSection.Closed(timeout, "conflictingUpdateSectionOtherInstance");
      var conflictingGetCopySectionSameInstance = GatedCodeSection.Closed(timeout, "conflictingGetCopySectionSameInstance");
      var conflictingGetCopySectionOtherInstance = GatedCodeSection.Closed(timeout, "conflictingGetCopySectionOtherInstance");

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
