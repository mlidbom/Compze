using System;
using System.Text;
using Compze.Utilities.SystemCE.IOCE;
using Compze.Utilities.Testing.DbPool.SystemCE.ThreadingCE;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public abstract class MachineWideSharedObject
{
   internal static readonly LazyCE<DirectoryCE> DataDirectory = new(() => DirectoryCE.StandardDirectories.LocalApplicationData.GetOrCreateDirectory("Compze").GetOrCreateDirectory("SharedFiles"));
}

public sealed class MachineWideSharedObject<TObject> : MachineWideSharedObject where TObject : class, new()
{
   readonly TextFile _file;
   readonly MutexCE _synchronizer;
   readonly ISharedObjectSerializer _serializer;

   internal static MachineWideSharedObject<TObject> For(string name, ISharedObjectSerializer serializer) => new(name, serializer);

   MachineWideSharedObject(string name, ISharedObjectSerializer serializer)
   {
      _serializer = serializer;
      var fileName = PathCE.ReplaceInvalidCharactersWith(name, '_');
      _synchronizer = MutexCE.ForMutexNamed(fileName);

      _file = _synchronizer.ExecuteWithLock(() => DataDirectory.Value.GetOrCreateTextFile(fileName, Encoding.UTF8, () => _serializer.Serialize(new TObject())));
   }

   internal TObject Update(Action<TObject> action) => _synchronizer.ExecuteWithLock(() =>
   {
      var instance = Load();
      action(instance);
      Save(instance);
      return instance;
   });

   internal TObject GetCopy() => _synchronizer.ExecuteWithLock(Load);

   internal void Delete() => _file.Delete();

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
         return _serializer.Deserialize<TObject>(json);
      }
      catch(Exception exception)
      {
         _file.Delete();
         _file.WriteAllText(_serializer.Serialize(new TObject()));
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
