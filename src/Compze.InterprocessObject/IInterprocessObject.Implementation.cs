using Compze.Internals.SystemCE.IOCE;
using Compze.Threading;
using Compze.Threading.Interprocess;

namespace Compze.InterprocessObject;

public partial interface IInterprocessObject
{
   private sealed class Implementation<TObject> : IInterprocessObject<TObject> where TObject : class
   {
      readonly IBinaryFile _file;
      readonly IAwaitableMutex _synchronizer;
      readonly IInterprocessObjectSerializer<TObject> _serializer;
      readonly Func<TObject> _createDefault;
      readonly CorruptionAction _corruptionAction;

      public Implementation(string name, bool isGlobal, DirectoryInfo directory, int maxBytes, IInterprocessObjectSerializer<TObject> serializer, Func<TObject> createDefault, CorruptionAction corruptionAction, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, ISignalPollingPolicy? signalPollingPolicy = null)
      {
         _serializer = serializer;
         _createDefault = createDefault;
         _corruptionAction = corruptionAction;
         var fileName = PathCE.ReplaceInvalidCharactersWith(name, '_');
         _synchronizer = isGlobal
            ? IAwaitableMutex.Global(fileName, directory, lockTimeout, waitTimeout, signalPollingPolicy)
            : IAwaitableMutex.Local(fileName, directory, lockTimeout, waitTimeout, signalPollingPolicy);

         _file = _synchronizer.Update(() =>
         {
            var file = new MemoryMappedBinaryFile(directory.File(fileName + ".mmf"), maxBytes);
            if(file.ReadAllBytes().Length == 0)
               file.WriteAllBytes(serializer.Serialize(createDefault()));
            return file;
         });
      }

      public IAwaitableCriticalSection CriticalSection => _synchronizer;
      public IAwaitableMutex Mutex => _synchronizer;

      public TResult Read<TResult>(Func<TObject, TResult> read, CancellationToken cancellationToken = default, LockTimeout? timeout = null)
      {
         using(_synchronizer.TakeReadLock(cancellationToken, timeout))
         {
            return read(Load());
         }
      }

      public TResult ReadWhen<TResult>(Func<TObject, bool> condition, Func<TObject, TResult> read, CancellationToken cancellationToken = default, WaitTimeout? timeout = null)
      {
         TObject? loaded = null;
         using(_synchronizer.TakeReadLockWhen(() => condition(loaded = Load()), cancellationToken, waitTimeout: timeout))
         {
            return read(loaded!);
         }
      }

      public TResult Update<TResult>(Func<TObject, TResult> update, CancellationToken cancellationToken = default, LockTimeout? timeout = null)
      {
         using(_synchronizer.TakeUpdateLock(cancellationToken, timeout))
         {
            var instance = Load();
            var result = update(instance);
            Save(instance);
            return result;
         }
      }

      public TResult UpdateWhen<TResult>(Func<TObject, bool> condition, Func<TObject, TResult> update, CancellationToken cancellationToken = default, WaitTimeout? timeout = null)
      {
         TObject? loaded = null;
         using(_synchronizer.TakeUpdateLockWhen(() => condition(loaded = Load()), cancellationToken, waitTimeout: timeout))
         {
            var result = update(loaded!);
            Save(loaded!);
            return result;
         }
      }

      public bool TryUpdateWhen(Func<TObject, bool> condition, Action<TObject> update, CancellationToken cancellationToken = default, WaitTimeout? timeout = null)
      {
         TObject? loaded = null;
         using var updateLock = _synchronizer.TryTakeUpdateLockWhen(() => condition(loaded = Load()), cancellationToken, waitTimeout: timeout);
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
               throw new Exception($"Failed to deserialize object from file {_file}",
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
