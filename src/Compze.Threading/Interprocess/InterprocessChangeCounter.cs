using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace Compze.Threading.Interprocess;

class InterprocessChangeCounter : IDisposable
{
   const int CounterSize = sizeof(long);

   readonly MemoryMappedFile _memoryMappedFile;
   readonly MemoryMappedViewAccessor _accessor;
   readonly unsafe long* _counterPointer;
   bool _disposed;

   public string Name { get; }

   public InterprocessChangeCounter(string name)
   {
      if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name must not be null, empty, or whitespace", nameof(name));

      Name = name;

      var backingFilePath = DeriveBackingFilePath(name);

      Directory.CreateDirectory(Path.GetDirectoryName(backingFilePath)!);

      #pragma warning disable CA2000
      var backingFileStream = new FileStream(
         backingFilePath,
         FileMode.OpenOrCreate,
         FileAccess.ReadWrite,
         FileShare.ReadWrite,
         bufferSize: 1,
         FileOptions.None);
#pragma warning restore CA2000

      if(backingFileStream.Length < CounterSize)
         backingFileStream.SetLength(CounterSize);

      _memoryMappedFile = MemoryMappedFile.CreateFromFile(
         backingFileStream,
         mapName: null,
         capacity: CounterSize,
         MemoryMappedFileAccess.ReadWrite,
         HandleInheritability.None,
         leaveOpen: false);

      _accessor = _memoryMappedFile.CreateViewAccessor(0, CounterSize, MemoryMappedFileAccess.ReadWrite);

      unsafe
      {
         //The point of this unsafe code is that it enables us to use Interlocked.* below, giving us atomic updates and reads without an interprocess mutex just for that.
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
