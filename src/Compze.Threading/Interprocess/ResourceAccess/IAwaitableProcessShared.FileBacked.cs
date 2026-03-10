using System.Text;
using Compze.Internals.SystemCE.Core.IOCE;

namespace Compze.Threading.Interprocess.ResourceAccess;

public partial interface IAwaitableProcessShared
{
   static readonly Lazy<DirectoryCE> DataDirectory = new(() => DirectoryCE.StandardDirectories
                                                                          .LocalApplicationData
                                                                          .GetOrCreateDirectory("Compze")
                                                                          .GetOrCreateDirectory("SharedFiles"));

   public static IFileBackedProcessShared<TShared> GlobalFileBacked<TShared>(
      string name,
      ISharedObjectSerializer<TShared> serializer,
      Func<TShared> createDefault,
      CorruptionAction corruptionAction) where TShared : class
      => new FileBackedProcessShared<TShared>(name, serializer, createDefault, corruptionAction);

   sealed class FileBackedProcessShared<TObject> : IFileBackedProcessShared<TObject> where TObject : class
   {
      readonly TextFile _file;
      readonly ISignalingAwaitableMutex _synchronizer;
      readonly ISharedObjectSerializer<TObject> _serializer;
      readonly Func<TObject> _createDefault;
      readonly CorruptionAction _corruptionAction;

      public FileBackedProcessShared(string name, ISharedObjectSerializer<TObject> serializer, Func<TObject> createDefault, CorruptionAction corruptionAction)
      {
         _serializer = serializer;
         _createDefault = createDefault;
         _corruptionAction = corruptionAction;
         var fileName = PathCE.ReplaceInvalidCharactersWith(name, '_');
         _synchronizer = ISignalingAwaitableMutex.Global(fileName);

         _file = _synchronizer.Update(() => DataDirectory.Value.GetOrCreateTextFile(fileName, Encoding.UTF8, CreateDefaultJson));
      }

      public IAwaitableLock Lock => _synchronizer;
      public IAwaitableMutex Mutex => _synchronizer;

      public TResult Read<TResult>(Func<TObject, TResult> read, LockTimeout? timeout = null)
      {
         using(_synchronizer.TakeReadLock(timeout))
         {
            return read(Load());
         }
      }

      public TResult ReadWhen<TResult>(Func<TObject, bool> condition, Func<TObject, TResult> read, WaitTimeout? timeout = null)
      {
         TObject? loaded = null;
         using(_synchronizer.TakeReadLockWhen(() => condition(loaded = Load()), timeout))
         {
            return read(loaded!);
         }
      }

      public TResult Update<TResult>(Func<TObject, TResult> update, LockTimeout? timeout = null)
      {
         using(_synchronizer.TakeUpdateLock(timeout))
         {
            var instance = Load();
            var result = update(instance);
            Save(instance);
            return result;
         }
      }

      public TResult UpdateWhen<TResult>(Func<TObject, bool> condition, Func<TObject, TResult> update, WaitTimeout? timeout = null)
      {
         TObject? loaded = null;
         using(_synchronizer.TakeUpdateLockWhen(() => condition(loaded = Load()), timeout))
         {
            var result = update(loaded!);
            Save(loaded!);
            return result;
         }
      }

      public bool TryUpdateWhen(Func<TObject, bool> condition, Action<TObject> action, WaitTimeout? timeout = null)
      {
         TObject? loaded = null;
         using var updateLock = _synchronizer.TryTakeUpdateLockWhen(() => condition(loaded = Load()), timeout);
         if(updateLock == null) return false;
         action(loaded!);
         Save(loaded!);
         return true;
      }

      public void Delete() => _file.GetFileInfo().Delete();

      string CreateDefaultJson() => _serializer.Serialize(_createDefault());

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
}
