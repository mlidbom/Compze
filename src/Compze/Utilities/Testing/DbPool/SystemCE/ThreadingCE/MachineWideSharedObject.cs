using System;
using System.IO;
using System.Text;
using Compze.Serialization.Newtonsoft;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Newtonsoft.Json;

namespace Compze.Utilities.Testing.DbPool.SystemCE.ThreadingCE;

public abstract class MachineWideSharedObject
{
   protected static readonly string DataFolder = CompzeTempFolder.EnsureFolderExists("SharedFiles");
   internal abstract void Delete();
}

public sealed class MachineWideSharedObject<TObject> : MachineWideSharedObject where TObject : class, new()
{
   readonly string _filePath;
   readonly MachineWideSingleThreaded _synchronizer;

   static string Serialize(TObject instance) => JsonConvert.SerializeObject(instance, Formatting.Indented, RenamingAndNonPublicMembersSupportingJsonSettings.Default);
   static TObject Deserialize(string serialized) => JsonConvert.DeserializeObject<TObject>(serialized, RenamingAndNonPublicMembersSupportingJsonSettings.Default).NotNull();

   internal static MachineWideSharedObject<TObject> For(string name) => new(name);

   MachineWideSharedObject(string name)
   {
      var fileName = name;
      // ReSharper disable once AccessToModifiedClosure
      Path.GetInvalidFileNameChars().ForEach(invalidChar => fileName = fileName.Replace(invalidChar, '_'));

      _filePath = Path.Combine(DataFolder, fileName);
      _synchronizer = MachineWideSingleThreaded.For($"{fileName}_mutex");
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

   internal override void Delete() => File.Delete(_filePath);

   void Save(TObject instance)
   {
      var json = Serialize(instance);
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
         return Deserialize(json);
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
