using System;
using System.IO;
using Composable.Contracts;
using Composable.Functional;
using Composable.Logging;
using Composable.Persistence;
using Composable.Serialization;
using Composable.SystemCE.LinqCE;
using Newtonsoft.Json;

namespace Composable.SystemCE.ThreadingCE;

class MachineWideSharedObject
{
   protected static readonly string DataFolder = ComposableTempFolder.EnsureFolderExists("SharedFiles");
}

class MachineWideSharedObject<TObject> : MachineWideSharedObject, IDisposable where TObject : new()
{
   class ReferenceCountingWrapper
   {
      public int References { get; set; } = 1;
      public TObject Object { get; set; } = new();
   }

   readonly string _filePath;
   readonly MachineWideSingleThreaded _synchronizer;
   bool _disposed;
   readonly bool _usePersistentFile;

   static string Serialize(ReferenceCountingWrapper instance) => JsonConvert.SerializeObject(instance, Formatting.Indented, JsonSettings.JsonSerializerSettings);
   static ReferenceCountingWrapper Deserialize(string serialized) => JsonConvert.DeserializeObject<ReferenceCountingWrapper>(serialized, JsonSettings.JsonSerializerSettings).NotNull();

   internal static MachineWideSharedObject<TObject> For(string name, bool usePersistentFile = false) => new(name, usePersistentFile);

   MachineWideSharedObject(string name, bool usePersistentFile)
   {
      var fileName = $"Composable_{name}";
      // ReSharper disable once AccessToModifiedClosure
      Path.GetInvalidFileNameChars().ForEach(invalidChar => fileName = fileName.Replace(invalidChar, '_'));

      _usePersistentFile = usePersistentFile;
      _filePath = Path.Combine(DataFolder, fileName);
      _synchronizer = MachineWideSingleThreaded.For($"{fileName}_mutex");

      _synchronizer.Execute(() =>
      {
         if(!File.Exists(_filePath))
         {
            Save(new ReferenceCountingWrapper());
         } else
         {
            Save(Load().mutate(it => it.References++));
         }
      });
   }

   internal TObject Update(Action<TObject> action)
   {
      Contract.Assert.That(!_disposed, "Attempt to use disposed object.");

      return _synchronizer.Execute(() =>
      {
         var wrapper = Load();
         action(wrapper.Object);
         Save(wrapper);
         return wrapper.Object;
      });
   }

   void Save(ReferenceCountingWrapper wrapper)
   {
      var json = Serialize(wrapper);
      File.WriteAllText(_filePath, json);
   }

   ReferenceCountingWrapper Load()
   {
      var json = File.ReadAllText(_filePath);
      try
      {
         return Deserialize(json);
      }catch(Exception exception)
      {
         var debugfile = _filePath + ".debug";
         this.Log().Warning($"Failed, attempting to save debug file: {debugfile}");
         File.WriteAllText(debugfile, json);
         this.Log().Warning($"Failed, successfully saved debug file: {debugfile}");
         throw;
      }
   }

   internal TObject GetCopy()
   {
      Contract.Assert.That(!_disposed, "Attempt to use disposed object.");

      return _synchronizer.Execute(() =>
      {
         if(!File.Exists(_filePath))
         {
            var referenceCountingWrapper = new ReferenceCountingWrapper();
            Save(referenceCountingWrapper);
            return referenceCountingWrapper.Object;
         }

         return Load().Object;
      });
   }

   public void Dispose()
   {
      if(!_disposed)
      {
         _synchronizer.Execute(() =>
         {
            if(File.Exists(_filePath))
            {
               var wrapper = Load();
               wrapper.References--;
               if(wrapper.References <= 0 && !_usePersistentFile)
               {
                  File.Delete(_filePath);
               } else
               {
                  Save(wrapper);
               }
            }
         });
      }

      _disposed = true;
   }
}
