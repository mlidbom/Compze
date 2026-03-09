using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace Compze.Threading.Interprocess;

class InterprocessChangeCounter : IDisposable
{
   const int CounterSize = sizeof(long);

   readonly string _backingFilePath;
#pragma warning disable CA2213 // MemoryMappedFile.CreateFromFile with leaveOpen:false takes ownership and disposes the stream
   readonly FileStream _backingFileStream;
#pragma warning restore CA2213
   readonly MemoryMappedFile _memoryMappedFile;
   readonly MemoryMappedViewAccessor _accessor;
   readonly unsafe long* _counterPointer;
   bool _disposed;

   public bool IsGlobal { get; }
   public string Name { get; }

   public InterprocessChangeCounter(string name, bool global)
   {
      if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name must not be null, empty, or whitespace", nameof(name));
      if(name.Contains('\\', StringComparison.Ordinal)) throw new ArgumentException("Name must not contain backslashes", nameof(name));

      IsGlobal = global;
      Name = global ? $@"Global\{name}" : $@"Local\{name}";

      _backingFilePath = DeriveBackingFilePath(Name);

      Directory.CreateDirectory(Path.GetDirectoryName(_backingFilePath)!);

      _backingFileStream = new FileStream(
         _backingFilePath,
         FileMode.OpenOrCreate,
         FileAccess.ReadWrite,
         FileShare.ReadWrite,
         bufferSize: 1,
         FileOptions.None);

      if(_backingFileStream.Length < CounterSize)
         _backingFileStream.SetLength(CounterSize);

      _memoryMappedFile = MemoryMappedFile.CreateFromFile(
         _backingFileStream,
         mapName: null,
         capacity: CounterSize,
         MemoryMappedFileAccess.ReadWrite,
         HandleInheritability.None,
         leaveOpen: false);

      _accessor = _memoryMappedFile.CreateViewAccessor(0, CounterSize, MemoryMappedFileAccess.ReadWrite);

      unsafe
      {
         byte* pointer = null;
         _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
         _counterPointer = (long*)(pointer + _accessor.PointerOffset);
      }
   }

   public unsafe void Increment()
   {
      ObjectDisposedException.ThrowIf(_disposed, this);
      Interlocked.Increment(ref Unsafe.AsRef<long>(_counterPointer));
   }

   public unsafe long Count
   {
      get
      {
         ObjectDisposedException.ThrowIf(_disposed, this);
         return Interlocked.Read(ref Unsafe.AsRef<long>(_counterPointer));
      }
   }

   static string DeriveBackingFilePath(string name)
   {
      var safeName = name.Replace('\\', '_');
      return Path.Combine(Path.GetTempPath(), "Compze", "Signals", safeName);
   }

   public void Dispose()
   {
      if(_disposed) return;
      _disposed = true;
      _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
      _accessor.Dispose();
      _memoryMappedFile.Dispose();
   }
}
