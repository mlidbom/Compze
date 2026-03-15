using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.InterprocessObject;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.xUnitBDD;
using JetBrains.Annotations;
using MemoryPack;
using static Compze.Must.MustActions;

// ReSharper disable ImplicitlyCapturedClosure

namespace Compze.DbPool.Tests.MachineWideState;

[MemoryPackable]
[UsedImplicitly] public partial class SharedObject
{
   // ReSharper disable once MemberCanBeInternal
   public string Name { get; set; } = "Default";
}

class SharedObjectSerializer : IInterprocessObjectSerializer<SharedObject>
{
   static SharedObjectSerializer()
   {
      // Force JIT + formatter registration so the cost in profiling isn't attributed to the first real call.
      var warmup = MemoryPackSerializer.Serialize(new SharedObject());
      MemoryPackSerializer.Deserialize<SharedObject>(warmup);
   }

   public byte[] Serialize(SharedObject instance) => MemoryPackSerializer.Serialize(instance);

   public SharedObject Deserialize(byte[] data) => MemoryPackSerializer.Deserialize<SharedObject>(data)._assert().NotNull();
}

class CorruptingSerializer : IInterprocessObjectSerializer<SharedObject>
{
   readonly SharedObjectSerializer _inner = new();
   public bool FailOnDeserialize { get; set; }

   public byte[] Serialize(SharedObject instance) => _inner.Serialize(instance);

   public SharedObject Deserialize(byte[] data) =>
      FailOnDeserialize ? throw new InvalidOperationException("Simulated deserialization corruption") : _inner.Deserialize(data);
}

public class MachineWideSharedObjectTests : UniversalTestBase
{
   static readonly DirectoryInfo TestDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "SharedObjects"))._mutate(it => it.Create());
   readonly List<IInterprocessObject<SharedObject>> _created = [];

   protected override void DisposeInternal() => _created.ForEach(obj => obj.Delete());

   IInterprocessObject<SharedObject> CreateAndDeleteFileWhenTestCompletes(string name)
   {
      var created = IInterprocessObject.NewGlobal(name, new SharedObjectSerializer(), () => new SharedObject(), CorruptionAction.ThrowException, maxBytes: 4 * 1024, TestDirectory);
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

   [XF] public void After_Delete_a_new_instance_with_the_same_name_gets_the_default_value()
   {
      var name = Guid.NewGuid().ToString();
      var shared = CreateAndDeleteFileWhenTestCompletes(name);

      shared.Update(it => it.Name = "Updated");
      shared.Read(it => it.Name.Must().Be("Updated"));

      shared.Delete();

      var fresh = CreateAndDeleteFileWhenTestCompletes(name);
      fresh.Read(it => it.Name.Must().Be("Default"));
   }

   [XF] public async Task Update_blocks_GetCopy_and_Update_from_both_same_and_other_instances()
   {
      var timeout = WaitTimeout.Seconds(15);
      var updateGate = IThreadGate.NewClosed(timeout, "updateGate");
      var conflictingUpdateSectionSameInstance = IGatedCodeSection.NewClosed(timeout, "conflictingUpdateSectionSameInstance");
      var conflictingUpdateSectionOtherInstance = IGatedCodeSection.NewClosed(timeout, "conflictingUpdateSectionOtherInstance");
      var conflictingGetCopySectionSameInstance = IGatedCodeSection.NewClosed(timeout, "conflictingGetCopySectionSameInstance");
      var conflictingGetCopySectionOtherInstance = IGatedCodeSection.NewClosed(timeout, "conflictingGetCopySectionOtherInstance");

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

public class When_deserialization_fails : UniversalTestBase
{
   static readonly DirectoryInfo TestDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "SharedObjects"))._mutate(it => it.Create());
   readonly List<IInterprocessObject<SharedObject>> _created = [];

   protected override void DisposeInternal() => _created.ForEach(obj => obj.Delete());

   IInterprocessObject<SharedObject> CreateWithCorruptionAction(string name, CorruptionAction corruptionAction, CorruptingSerializer serializer)
   {
      var created = IInterprocessObject.NewGlobal(name, serializer, () => new SharedObject(), corruptionAction, maxBytes: 4 * 1024, TestDirectory);
      _created.Add(created);
      return created;
   }

   public class and_CorruptionAction_is_ReplaceContentWithDefaultAndThrow : When_deserialization_fails
   {
      readonly CorruptingSerializer _serializer = new();
      readonly string _name = Guid.NewGuid().ToString();
      IInterprocessObject<SharedObject> _shared = null!;

      void SetUpCorruptedObject()
      {
         if(_shared != null!) return;
         _shared = CreateWithCorruptionAction(_name, CorruptionAction.ReplaceContentWithDefaultAndThrow, _serializer);
         _shared.Update(it => it.Name = "Modified");
         _serializer.FailOnDeserialize = true;
      }

      [XF] public void throws_exception_mentioning_replacement()
      {
         SetUpCorruptedObject();
         Invoking(() => _shared.Read(it => it.Name))
           .Must().Throw<Exception>()
           .Which.Message.Must().Contain("Deleted the corrupt file");
      }

      [XF] public void replaces_content_with_default_so_next_read_succeeds()
      {
         SetUpCorruptedObject();
         try { _shared.Read(it => it.Name); } catch { /* expected */ }
         _serializer.FailOnDeserialize = false;
         _shared.Read(it => it.Name).Must().Be("Default");
      }
   }

   public class and_CorruptionAction_is_ThrowException : When_deserialization_fails
   {
      readonly CorruptingSerializer _serializer = new();
      readonly string _name = Guid.NewGuid().ToString();
      IInterprocessObject<SharedObject> _shared = null!;

      void SetUpCorruptedObject()
      {
         if(_shared != null!) return;
         _shared = CreateWithCorruptionAction(_name, CorruptionAction.ThrowException, _serializer);
         _shared.Update(it => it.Name = "Modified");
         _serializer.FailOnDeserialize = true;
      }

      [XF] public void throws_exception_without_modifying_the_backing_file()
      {
         SetUpCorruptedObject();
         Invoking(() => _shared.Read(it => it.Name)).Must().Throw<Exception>();
         _serializer.FailOnDeserialize = false;
         _shared.Read(it => it.Name).Must().Be("Modified");
      }
   }
}
