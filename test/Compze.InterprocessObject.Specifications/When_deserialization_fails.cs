using Compze.InterprocessObject.Specifications.TestInfrastructure;
using Compze.Must;
using Compze.Must.Assertions;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;

#pragma warning disable CA1711 // BDD spec context class names mirror the enum value being tested (e.g. `and_CorruptionAction_is_ThrowException`); the 'Exception' suffix is incidental, not a type-naming choice.

namespace Compze.InterprocessObject.Specifications;

public class When_deserialization_fails : UniversalTestBase
{
   static DirectoryInfo TestDirectory => new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "SharedObjects"))._mutate(it => it.Create());
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
         MustActions.Invoking(() => _shared.Read(it => it.Name))
                    .Must().Throw<Exception>()
                    .Which.Message.Must().Contain("Deleted the corrupt file");
      }

      [XF] public void replaces_content_with_default_so_next_read_succeeds()
      {
         SetUpCorruptedObject();
         MustActions.Invoking(() => _shared.Read(it => it.Name)).Must().Throw<Exception>();
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
         MustActions.Invoking(() => _shared.Read(it => it.Name)).Must().Throw<Exception>();
         _serializer.FailOnDeserialize = false;
         _shared.Read(it => it.Name).Must().Be("Modified");
      }
   }
}
