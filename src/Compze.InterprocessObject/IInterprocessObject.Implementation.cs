using Compze.Internals.SystemCE.Core.IOCE;
using Compze.Threading;
using Compze.Threading.Interprocess;
using Compze.Threading.ResourceAccess;

namespace Compze.InterprocessObject;

public partial interface IInterprocessObject
{
   sealed class InterprocessObjectImplementation<TObject> : IInterprocessObject<TObject> where TObject : class
   {
      readonly IBinaryFile _file;
      readonly ISignalingAwaitableMutex _synchronizer;
      readonly IInterprocessObjectSerializer<TObject> _serializer;
      readonly Func<TObject> _createDefault;
      readonly CorruptionAction _corruptionAction;

      public InterprocessObjectImplementation(string name, Func<string, IBinaryFile> createBinaryFile, IInterprocessObjectSerializer<TObject> serializer, Func<TObject> createDefault, CorruptionAction corruptionAction)
      {
         _serializer = serializer;
         _createDefault = createDefault;
         _corruptionAction = corruptionAction;
         var fileName = PathCE.ReplaceInvalidCharactersWith(name, '_');
         _synchronizer = ISignalingAwaitableMutex.Global(fileName);

         _file = _synchronizer.Update(() =>
         {
            var file = createBinaryFile(fileName);
            if(file.ReadAllBytes().Length == 0)
               file.WriteAllBytes(serializer.Serialize(createDefault()));
            return file;
         });
      }

      public IAwaitableCriticalSection CriticalSection => _synchronizer;
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

      public bool TryUpdateWhen(Func<TObject, bool> condition, Action<TObject> update, WaitTimeout? timeout = null)
      {
         TObject? loaded = null;
         using var updateLock = _synchronizer.TryTakeUpdateLockWhen(() => condition(loaded = Load()), timeout);
         if(updateLock == null) return false;
         update(loaded!);
         Save(loaded!);
         return true;
      }

      public void Delete() => _file.Delete();

      public void Dispose() => _synchronizer.Dispose();

      void Save(TObject instance)
      {
         var serialized = _serializer.Serialize(instance);
         _file.WriteAllBytes(serialized);
      }

      TObject Load()
      {
         var data = _file.ReadAllBytes();
         try
         {
            return _serializer.Deserialize(data);
         }
         catch(Exception exception)
         {
            if(_corruptionAction != CorruptionAction.ReplaceContentWithDefaultAndThrow)
               throw new Exception($"""Failed to deserialize object from file {_file}""",
                                   exception);

            _file.Delete();
            _file.WriteAllBytes(_serializer.Serialize(_createDefault()));

            throw new Exception($"""
                                 Failed to deserialize object from file {_file}
                                 Deleted the corrupt file and replaced it with the content of a default {typeof(TObject).FullName}.
                                 """,
                                exception);
         }
      }
   }
}
