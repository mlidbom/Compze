using System;
using System.IO;
using System.Text;
using Compze.Serialization.Newtonsoft;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Newtonsoft.Json;

namespace Compze.Utilities.Testing.DbPool.SystemCE.ThreadingCE;

    public abstract class MachineWideSharedObject
    {
       protected static readonly string DataFolder = CompzeTempFolder.EnsureFolderExists("SharedFiles");
       internal abstract void Delete();
    }

    public sealed class MachineWideSharedObject<TObject> : MachineWideSharedObject where TObject : new()
    {
       class ReferenceCountingWrapper
       {
          public int References { get; set; } = 1;
          public TObject Object { get; set; } = new();
       }

       readonly string _filePath;
       readonly MachineWideSingleThreaded _synchronizer;

       static string Serialize(ReferenceCountingWrapper instance) => JsonConvert.SerializeObject(instance, Formatting.Indented, RenamingAndNonPublicMembersSupportingJsonSettings.Default);
       static ReferenceCountingWrapper Deserialize(string serialized) => JsonConvert.DeserializeObject<ReferenceCountingWrapper>(serialized, RenamingAndNonPublicMembersSupportingJsonSettings.Default).NotNull();

       internal static MachineWideSharedObject<TObject> For(string name) => new(name);

       MachineWideSharedObject(string name)
       {
          var fileName = name;
          // ReSharper disable once AccessToModifiedClosure
          Path.GetInvalidFileNameChars().ForEach(invalidChar => fileName = fileName.Replace(invalidChar, '_'));

          _filePath = Path.Combine(DataFolder, fileName);
          _synchronizer = MachineWideSingleThreaded.For($"{fileName}_mutex");

          _synchronizer.Execute(() =>
          {
             Save(File.Exists(_filePath)
                     ? Load().mutate(it => it.References++)
                     : new ReferenceCountingWrapper());
          });
       }


       internal override void Delete() => File.Delete(_filePath);

       internal TObject Update(Action<TObject> action) => _synchronizer.Execute(() =>
       {
          var wrapper = Load();
          action(wrapper.Object);
          Save(wrapper);
          return wrapper.Object;
       });

       internal TObject GetCopy() => _synchronizer.Execute(() => Load().Object);

       void Save(ReferenceCountingWrapper wrapper)
       {
          var json = Serialize(wrapper);
          File.WriteAllText(_filePath, json, Encoding.UTF8);
       }

       ReferenceCountingWrapper Load()
       {
          var json = File.ReadAllText(_filePath, Encoding.UTF8);
          try
          {
             return Deserialize(json);
          }
          catch(Exception exception)
          {
             File.Delete(_filePath);
             throw new Exception($"""
                                  
                                  Failed to deserialize object from file {_filePath}
                                  Deleted the apparently corrupt file.
                                  The file content was: 
                                  
                                  {json}
                                   
                                  """, exception);
          }
       }
    }
