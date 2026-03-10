using Compze.Contracts;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading;
using Compze.Threading.Interprocess.ResourceAccess;
using Compze.Threading.Testing;
using Compze.xUnitBDD;
using JetBrains.Annotations;
using Newtonsoft.Json;

// ReSharper disable ImplicitlyCapturedClosure

namespace Compze.DbPool.Tests.MachineWideState;

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
   readonly List<IFileBackedProcessShared<SharedObject>> _created = [];

   protected override void DisposeInternal() => _created.ForEach(obj => obj.Delete());

   IFileBackedProcessShared<SharedObject> CreateAndDeleteFileWhenTestCompletes(string name)
   {
      var created = IAwaitableProcessShared.GlobalFileBacked(name, new SharedObjectSerializer(), () => new SharedObject(), CorruptionAction.ThrowException);
      _created.Add(created);
      return created;
   }

   [XF] public void Create()
   {
      var name = Guid.NewGuid().ToString();
      var shared = CreateAndDeleteFileWhenTestCompletes(name);
      shared.Read(it => it.Name.Must().Be("Default"));
   }

   [XF] public void Create_update_and_get()
   {
      var name = Guid.NewGuid().ToString();
      var shared = CreateAndDeleteFileWhenTestCompletes(name);
      shared.Read(it => it.Name.Must().Be("Default"));

      shared.Update(it => it.Name = "Updated");

      shared.Read(it => it.Name.Must().Be("Updated"));

      shared.Read(it => it.Name.Must().Be("Updated"));
   }

   [XF] public void Two_instances_with_same_name_share_data()
   {
      var name = Guid.NewGuid().ToString();
      var shared1 = CreateAndDeleteFileWhenTestCompletes(name);
      var shared2 = CreateAndDeleteFileWhenTestCompletes(name);

      shared1.Read(it => it.Name.Must().Be("Default"));
      shared2.Read(it => it.Name.Must().Be("Default"));

      shared1.Update(it => it.Name = "Updated");

      shared1.Read(it => it.Name.Must().Be("Updated"));
      shared2.Read(it => it.Name.Must().Be("Updated"));
   }

   [XF] public void Persistent_Once_all_instance_are_disposed_data_is_retained()
   {
      const string name = "40BD77DF-7C32-4B28-9A49-DA2CE202CC4F";
      var newName = Guid.NewGuid().ToString();
      var shared = CreateAndDeleteFileWhenTestCompletes(name);

      shared.Update(it => it.Name = newName);
      shared.Read(it => it.Name.Must().Be(newName));
      var shared2 = CreateAndDeleteFileWhenTestCompletes(name);
      shared.Read(it => it.Name.Must().Be(newName));

      shared2.Read(it => it.Name.Must().Be(newName));

      shared = CreateAndDeleteFileWhenTestCompletes(name);
      shared.Read(it => it.Name.Must().Be(newName));
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
         TaskCE.Run(() => conflictingGetCopySectionSameInstance.Execute(() => shared1.Read(_ => {}))),
         TaskCE.Run(() => conflictingUpdateSectionOtherInstance.Execute(() => shared2.Update(_ => {}))),
         TaskCE.Run(() => conflictingGetCopySectionOtherInstance.Execute(() => shared2.Read(_ => {}))));

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
