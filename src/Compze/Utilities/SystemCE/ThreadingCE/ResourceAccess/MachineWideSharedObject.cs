using System;
using System.IO;
using System.Text;
using Compze.Utilities.SystemCE.IOCE;
using Compze.Utilities.Testing.DbPool.SystemCE.ThreadingCE;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

static class CompzeFolder
{
   static readonly MutexCE MachineWideLock = MutexCE.ForMutexNamed(nameof(CompzeFolder));
   static readonly string DefaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Compze");
   static readonly string FolderPath = EnsureFolderExists();

   internal static string EnsureFolderExists(string folderName) => MachineWideLock.ExecuteWithLock(() =>
   {
      var folder = Path.Combine(FolderPath, folderName);
      if(!Directory.Exists(folder))
      {
         Directory.CreateDirectory(folder);
      }

      return folder;
   });

   static string EnsureFolderExists()
   {
      return MachineWideLock.ExecuteWithLock(() =>
      {
         if(!Directory.Exists(DefaultPath))
         {
            Directory.CreateDirectory(DefaultPath);
         }
         return DefaultPath;
      });
   }
}

public abstract class MachineWideSharedObject
{
   protected static readonly string DataFolder = CompzeFolder.EnsureFolderExists("SharedFiles");
}

public sealed class MachineWideSharedObject<TObject> : MachineWideSharedObject where TObject : class, new()
{
   readonly string _filePath;
   readonly MutexCE _synchronizer;
   readonly ISharedObjectSerializer _serializer;

   internal static MachineWideSharedObject<TObject> For(string name, ISharedObjectSerializer serializer) => new(name, serializer);

   MachineWideSharedObject(string name, ISharedObjectSerializer serializer)
   {
      _serializer = serializer;
      var fileName = PathCE.ReplaceInvalidCharactersWith(name, '_');
      _filePath = Path.Combine(DataFolder, fileName);
      _synchronizer = MutexCE.ForMutexNamed(fileName);
      _synchronizer.ExecuteWithLock(EnsureFileExists);
   }

   internal TObject Update(Action<TObject> action) => _synchronizer.ExecuteWithLock(() =>
   {
      var instance = Load();
      action(instance);
      Save(instance);
      return instance;
   });

   internal TObject GetCopy() => _synchronizer.ExecuteWithLock(Load);

   internal void Delete() => File.Delete(_filePath);

   void Save(TObject instance)
   {
      var json = _serializer.Serialize(instance);
      File.WriteAllText(_filePath, json, Encoding.UTF8);
   }

   void EnsureFileExists()
   {
      if(!File.Exists(_filePath))
      {
         Save(new TObject());
      }
   }

   TObject Load()
   {
      var json = File.ReadAllText(_filePath, Encoding.UTF8);
      try
      {
         return _serializer.Deserialize<TObject>(json);
      }
      catch(Exception exception)
      {
         File.Delete(_filePath);
         EnsureFileExists();
         throw new Exception($"""

                              Failed to deserialize object from file {_filePath}
                              Deleted the corrupt file and replaced it with the content of a default {typeof(TObject).FullName}.
                              The file content was: 

                              {json}
                               
                              """,
                             exception);
      }
   }
}
