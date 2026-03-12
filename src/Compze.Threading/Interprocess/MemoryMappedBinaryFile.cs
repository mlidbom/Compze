using System.IO.MemoryMappedFiles;
using Compze.Internals.SystemCE.Core.IOCE;

namespace Compze.Threading.Interprocess;

class MemoryMappedBinaryFile : IBinaryFile, IDisposable
{
   const int HeaderSize = sizeof(int);

   readonly string _filePath;
   readonly int _maxCapacityInBytes;

   MemoryMappedFile? _memoryMappedFile;
   MemoryMappedViewAccessor? _accessor;
   unsafe byte* _basePointer;

   public MemoryMappedBinaryFile(string filePath, int maxCapacityInBytes)
   {
      _filePath = filePath;
      _maxCapacityInBytes = maxCapacityInBytes;
      EnsureMapping();
   }

   public unsafe byte[] ReadAllBytes()
   {
      EnsureMapping();
      var length = *(int*)_basePointer;
      if(length == 0) return [];
      var data = new byte[length];
      new ReadOnlySpan<byte>(_basePointer + HeaderSize, length).CopyTo(data);
      return data;
   }

   public unsafe void WriteAllBytes(byte[] bytes)
   {
      if(bytes.Length > _maxCapacityInBytes)
         throw new InvalidOperationException($"Data size {bytes.Length} exceeds maximum capacity {_maxCapacityInBytes} bytes");
      EnsureMapping();
      *(int*)_basePointer = bytes.Length;
      bytes.AsSpan().CopyTo(new Span<byte>(_basePointer + HeaderSize, bytes.Length));
   }

   public void Delete()
   {
      DisposeMapping();
#pragma warning disable CA1031 // Best-effort file deletion — file may still be held by another mapping to the same backing file
      try { if(File.Exists(_filePath)) File.Delete(_filePath); }
      catch(IOException) { }
#pragma warning restore CA1031
   }

   public void Dispose()
   {
      DisposeMapping();
      GC.SuppressFinalize(this);
   }

   unsafe void EnsureMapping()
   {
      if(_memoryMappedFile != null) return;

      Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

      var totalSize = HeaderSize + _maxCapacityInBytes;

#pragma warning disable CA2000
      var backingFileStream = new FileStream(
         _filePath,
         FileMode.OpenOrCreate,
         FileAccess.ReadWrite,
         FileShare.ReadWrite,
         bufferSize: 1,
         FileOptions.None);
#pragma warning restore CA2000

      if(backingFileStream.Length < totalSize)
         backingFileStream.SetLength(totalSize);

      _memoryMappedFile = MemoryMappedFile.CreateFromFile(
         backingFileStream,
         mapName: null,
         capacity: totalSize,
         MemoryMappedFileAccess.ReadWrite,
         HandleInheritability.None,
         leaveOpen: false);

      _accessor = _memoryMappedFile.CreateViewAccessor(0, totalSize, MemoryMappedFileAccess.ReadWrite);

      byte* pointer = null;
      _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
      _basePointer = pointer + _accessor.PointerOffset;
   }

   unsafe void DisposeMapping()
   {
      if(_accessor != null)
      {
         _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
         _accessor.Dispose();
      }
      _memoryMappedFile?.Dispose();
      _accessor = null;
      _memoryMappedFile = null;
      _basePointer = null;
   }
}
