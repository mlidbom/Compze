using System.Text;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.IOCE;
using Compze.Threading;
using Compze.Threading.Interprocess;
using Compze.Threading.Interprocess.ResourceAccess;

namespace Compze.DbPool.MachineWideState;

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

public interface IMachineWideSharedObject<out TObject> : IAwaitableProcessShared<TObject>
{
   void Delete();
}

public sealed class MachineWideSharedObject<TObject> : MachineWideSharedObject, IMachineWideSharedObject<TObject> where TObject : class, new()
{
   readonly TextFile _file;
   readonly ISignalingAwaitableMutex _synchronizer;
   readonly ISharedObjectSerializer<TObject> _serializer;
   readonly CorruptionAction _corruptionAction;

   public static IMachineWideSharedObject<TObject> For(string name, ISharedObjectSerializer<TObject> serializer, CorruptionAction corruptionAction) => new MachineWideSharedObject<TObject>(name, serializer, corruptionAction);

   MachineWideSharedObject(string name, ISharedObjectSerializer<TObject> serializer, CorruptionAction corruptionAction)
   {
      _serializer = serializer;
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

   public TObject GetCopy() => Read(obj => obj);

   public void Delete() => _file.GetFileInfo().Delete();

   string CreateDefaultJson() => _serializer.Serialize(new TObject());

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
