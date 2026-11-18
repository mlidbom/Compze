using System.Text;
using Compze.Teventive.StateStore._internal;
using Compze.Threading.ResourceAccess;

namespace Compze.Teventive.StateStore._private.Files;

///<summary>Files kept in memory, for specifications: a state store composed with <see cref="TaggregateFilesBuilder.KeptInMemory"/> writes and<br/>
/// reads its files here instead of on the disk, one such file system for each container. It is also the<br/>
/// <see cref="ITaggregateFilesInMemory"/> through which a specification reads and changes them.</summary>
///<remarks>A folder exists only as the paths of the files in it. Paths are compared as full paths, ignoring case, as on Windows.<br/>
/// Each write gives the file a last write time later than any given before, so that two writes within one tick of the system's<br/>
/// clock still differ, as the store needs to tell a changed file, at a reload and from a cache of its start.</remarks>
sealed class FilesInMemory : IFileSystem, ITaggregateFilesInMemory
{
   static readonly UTF8Encoding Utf8WithoutByteOrderMark = new(encoderShouldEmitUTF8Identifier: false);
   readonly IThreadShared<NonThreadSafeFiles> _files = IThreadShared.New(new NonThreadSafeFiles());

   public IReadOnlyCollection<string> Paths => _files.Locked(IReadOnlyCollection<string> (it) => [..it.ByPath.Keys]);

   public bool FileExists(string file) => _files.Locked(it => it.ByPath.ContainsKey(Full(file)));

   public bool FolderExists(string folder) => _files.Locked(it => it.ByPath.Keys.Any(path => IsIn(path, folder, includingSubfolders: true)));

   public void CreateFolder(string folder) {} //A folder exists only as the paths of the files in it.

   public IReadOnlyList<ListedFile> FilesIn(string folder, bool includingSubfolders) =>
      _files.Locked(IReadOnlyList<ListedFile> (it) =>
                       [..it.ByPath.Where(file => IsIn(file.Key, folder, includingSubfolders))
                            .Select(file => new ListedFile(file.Key, Path.GetRelativePath(Full(folder), file.Key), file.Value.Content.Length, file.Value.LastWriteTimeUtc))]);

   public FileStamp StampOf(string file) =>
      _files.Locked(it => it.ByPath.TryGetValue(Full(file), out var stored) ? new FileStamp(stored.Content.Length, stored.LastWriteTimeUtc) : throw NoSuchFile(file));

   public string ReadAllText(string file)
   {
      using var reader = new StreamReader(new MemoryStream(ReadAllBytes(file)), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
      return reader.ReadToEnd();
   }

   public byte[] ReadAllBytes(string file) => _files.Locked(it => it.ByPath.TryGetValue(Full(file), out var stored) ? stored.Content.ToArray() : throw NoSuchFile(file));

   public void WriteAllText(string file, string content) => WriteAllBytes(file, Utf8WithoutByteOrderMark.GetBytes(content));

   public void WriteAllBytes(string file, byte[] content) => _files.Locked(it => it.Write(Full(file), [..content]));

   public void Move(string from, string to, bool overwrite) =>
      _files.Locked(it =>
      {
         var (fullFrom, fullTo) = (Full(from), Full(to));
         if(!it.ByPath.Remove(fullFrom, out var moved)) throw NoSuchFile(from);
         if(!overwrite && it.ByPath.ContainsKey(fullTo))
         {
            it.ByPath.Add(fullFrom, moved);
            throw new IOException($"Cannot create a file when that file already exists: '{to}'.");
         }

         it.ByPath.Remove(fullTo);
         it.ByPath.Add(fullTo, moved);
      });

   public void Delete(string file) => _files.Locked(it => it.ByPath.Remove(Full(file)));

   static string Full(string path) => Path.GetFullPath(path);

   static bool IsIn(string file, string folder, bool includingSubfolders)
   {
      var folderWithSeparator = Path.TrimEndingDirectorySeparator(Full(folder)) + Path.DirectorySeparatorChar;
      return file.StartsWith(folderWithSeparator, StringComparison.OrdinalIgnoreCase)
          && (includingSubfolders || !file.AsSpan(folderWithSeparator.Length).Contains(Path.DirectorySeparatorChar));
   }

   static FileNotFoundException NoSuchFile(string file) => new($"Could not find file '{file}'.", file);

   sealed class NonThreadSafeFiles
   {
      DateTime _lastWriteTimeUtc = DateTime.MinValue;

      internal Dictionary<string, StoredFile> ByPath { get; } = new(StringComparer.OrdinalIgnoreCase);

      internal void Write(string file, byte[] content)
      {
         var now = DateTime.UtcNow;
         _lastWriteTimeUtc = now > _lastWriteTimeUtc ? now : _lastWriteTimeUtc.AddTicks(1);
         ByPath.Remove(file);
         ByPath.Add(file, new StoredFile(content, _lastWriteTimeUtc));
      }
   }

   ///<summary>A file's content, and when it was last written.</summary>
   sealed class StoredFile(byte[] content, DateTime lastWriteTimeUtc)
   {
      internal byte[] Content { get; } = content;
      internal DateTime LastWriteTimeUtc { get; } = lastWriteTimeUtc;
   }
}
