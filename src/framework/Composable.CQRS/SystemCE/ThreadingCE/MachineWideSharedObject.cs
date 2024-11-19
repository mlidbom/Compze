using System;
using System.IO;
using Composable.Contracts;
using Composable.Persistence;
using Composable.Serialization;

namespace Composable.SystemCE.ThreadingCE
{
    class MachineWideSharedObjectFiles
    {
        protected static readonly string DataFolder = ComposableTempFolder.EnsureFolderExists("SharedFiles");
    }

    class MachineWideSharedObjectFiles<TObject> : MachineWideSharedObjectFiles, IDisposable where TObject : BinarySerialized<TObject>
    {
       readonly string _filePath;
       readonly MachineWideSingleThreaded _synchronizer;
       bool _disposed;
       readonly bool _usePersistentFile;

       internal static MachineWideSharedObjectFiles<TObject> For(string name, bool usePersistentFile = false) => new MachineWideSharedObjectFiles<TObject>(name, usePersistentFile);

       MachineWideSharedObjectFiles(string name, bool usePersistentFile)
       {
          var fileName = $"Composable_{name}";
          foreach(var invalidChar in Path.GetInvalidFileNameChars())
             fileName = fileName.Replace(invalidChar, '_');

          _usePersistentFile = usePersistentFile;
          _filePath = Path.Combine(DataFolder, fileName);
          _synchronizer = MachineWideSingleThreaded.For($"{fileName}_mutex");

          _synchronizer.Execute(() =>
          {
             if(!File.Exists(_filePath))
             {
                Set(BinarySerialized<TObject>.DefaultConstructor(), 1);
             } else
             {
                var (_, refCount) = ReadFile();
                Set(GetCopy(), refCount + 1);
             }
          });
       }

       internal TObject Update(Action<TObject> action)
       {
          Contract.Assert.That(!_disposed, "Attempt to use disposed object.");

          return _synchronizer.Execute(() =>
          {
             var (instance, refCount) = ReadFile();
             action(instance);
             Set(instance, refCount);
             return instance;
          });
       }

       void Set(TObject value, int refCount)
       {
          using var stream = new MemoryStream();
          using var writer = new BinaryWriter(stream);
          writer.Write(refCount);
          value.Serialize(writer);
          File.WriteAllBytes(_filePath, stream.ToArray());
       }

       (TObject instance, int refCount) ReadFile()
       {
          var buffer = File.ReadAllBytes(_filePath);
          using var stream = new MemoryStream(buffer);
          using var reader = new BinaryReader(stream);
          var refCount = reader.ReadInt32();
          return (BinarySerialized<TObject>.DeserializeReader(reader), refCount);
       }

       internal TObject GetCopy()
       {
          Contract.Assert.That(!_disposed, "Attempt to use disposed object.");

          return _synchronizer.Execute(() =>
          {
             if(!File.Exists(_filePath))
             {
                var value = BinarySerialized<TObject>.DefaultConstructor();
                Set(value, 1);
                return value;
             }

             return ReadFile().instance;
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
                   var (instance, refCount) = ReadFile();
                   refCount--;
                   if(refCount <= 0 && !_usePersistentFile)
                   {
                      File.Delete(_filePath);
                   } else
                   {
                      Set(instance, refCount);
                   }
                }
             });
          }

          _disposed = true;
       }
    }
}