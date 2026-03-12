using Compze.Internals.SystemCE.Core.IOCE;

namespace Compze.Threading.Interprocess.ResourceAccess;

public partial interface IAwaitableProcessShared
{
   static readonly Lazy<DirectoryCE> DataDirectory = new(() => DirectoryCE.StandardDirectories
                                                                          .LocalApplicationData
                                                                          .GetOrCreateDirectory("Compze")
                                                                          .GetOrCreateDirectory("SharedFiles"));

   private sealed class FileBackedProcessShared<TObject> : IFileBackedProcessShared<TObject> where TObject : class
   {
      readonly IBinaryFile _file;
      readonly ISignalingAwaitableMutex _synchronizer;
      readonly ISharedObjectSerializer<TObject> _serializer;
      readonly Func<TObject> _createDefault;
      readonly CorruptionAction _corruptionAction;

      public FileBackedProcessShared(ISignalingAwaitableMutex synchronizer, IBinaryFile file, ISharedObjectSerializer<TObject> serializer, Func<TObject> createDefault, CorruptionAction corruptionAction)
      {
         _synchronizer = synchronizer;
         _file = file;
         _serializer = serializer;
         _createDefault = createDefault;
         _corruptionAction = corruptionAction;
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

      public bool TryUpdateWhen(Func<TObject, bool> condition, Action<TObject> action, WaitTimeout? timeout = null)
      {
         TObject? loaded = null;
         using var updateLock = _synchronizer.TryTakeUpdateLockWhen(() => condition(loaded = Load()), timeout);
         if(updateLock == null) return false;
         action(loaded!);
         Save(loaded!);
         return true;
      }

      public void Delete() => _file.Delete();

      void Save(TObject instance) => _file.WriteAllBytes(_serializer.Serialize(instance));

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
