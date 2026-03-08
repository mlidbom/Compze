using System.Text;
using Compze.Threading;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.IOCE;
using Compze.Threading.Interprocess;

namespace Compze.DbPool;

public abstract class MachineWideSharedObject
{
   internal static readonly LazyCE<DirectoryCE> DataDirectory = new(() => DirectoryCE.StandardDirectories
                                                                                     .LocalApplicationData
                                                                                     .GetOrCreateDirectory("Compze")
                                                                                     .GetOrCreateDirectory("SharedFiles"));
}

public enum CorruptionAction
{
   ThrowException = 0,
   ReplaceContentWithDefaultAndThrow = 1
}

public sealed class MachineWideSharedObject<TObject> : MachineWideSharedObject where TObject : class, new()
{
   readonly TextFile _file;
   readonly MutexCE _synchronizer;
   readonly ISharedObjectSerializer<TObject> _serializer;
   readonly CorruptionAction _corruptionAction;

   public static MachineWideSharedObject<TObject> For(string name, ISharedObjectSerializer<TObject> serializer, CorruptionAction corruptionAction) => new(name, serializer, corruptionAction);

   MachineWideSharedObject(string name, ISharedObjectSerializer<TObject> serializer, CorruptionAction corruptionAction)
   {
      _serializer = serializer;
      _corruptionAction = corruptionAction;
      var fileName = PathCE.ReplaceInvalidCharactersWith(name, '_');
      _synchronizer = MutexCE.GlobalNamed(fileName);

      _file = _synchronizer.Locked(() => DataDirectory.Value.GetOrCreateTextFile(fileName, Encoding.UTF8, CreateDefaultJson));
   }

   string CreateDefaultJson() => _serializer.Serialize(new TObject());

   public TObject Update(Action<TObject> action) => _synchronizer.Locked(() =>
   {
      var instance = Load();
      action(instance);
      Save(instance);
      return instance;
   });

   public TObject GetCopy() => _synchronizer.Locked(Load);

   public void Delete() => _file.GetFileInfo().Delete();

   void Save(TObject instance)
   {
      var json = _serializer.Serialize(instance);
      _file.WriteAllText(json);
   }

   TObject Load()
   {
      var json = _file.ReadAllText();
      try
      {
         return _serializer.Deserialize(json);
      }
      catch(Exception exception)
      {
         if(_corruptionAction != CorruptionAction.ReplaceContentWithDefaultAndThrow)
            throw new Exception($"""Failed to deserialize object from file {_file}""",
                                exception);

         _file.GetFileInfo().Delete();
         var defaultJson = CreateDefaultJson();
         _file.WriteAllText(defaultJson);

         throw new Exception($"""

                              Failed to deserialize object from file {_file}
                              Deleted the corrupt file and replaced it with the content of a default {typeof(TObject).FullName}.
                              The file content was: 

                              {json}
                               
                              """,
                             exception);
      }
   }
}
