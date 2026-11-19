using Compze.Teventive.MemoryImageStateStore._internal;

namespace Compze.Teventive.MemoryImageStateStore._private.Files;

///<summary>The disk, as the file system of a memory image state store kept in files: each operation is the <see cref="File"/> or<br/>
/// <see cref="Directory"/> method of the same name.</summary>
sealed class FilesOnDisk : IFileSystem
{
   internal static readonly FilesOnDisk Instance = new();

   FilesOnDisk() {}

   public bool FileExists(string file) => File.Exists(file);

   public bool FolderExists(string folder) => Directory.Exists(folder);

   public void CreateFolder(string folder) => Directory.CreateDirectory(folder);

   public IReadOnlyList<ListedFile> FilesIn(string folder, bool includingSubfolders) =>
      [..new DirectoryInfo(folder).EnumerateFiles("*", includingSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                                  .Select(it => new ListedFile(it.FullName, Path.GetRelativePath(folder, it.FullName), it.Length, it.LastWriteTimeUtc))];

   public FileStamp StampOf(string file)
   {
      var info = new FileInfo(file);
      return new FileStamp(info.Length, info.LastWriteTimeUtc);
   }

   public string ReadAllText(string file) => File.ReadAllText(file);

   public byte[] ReadAllBytes(string file) => File.ReadAllBytes(file);

   public void WriteAllText(string file, string content) => File.WriteAllText(file, content);

   public void WriteAllBytes(string file, byte[] content) => File.WriteAllBytes(file, content);

   public void Move(string from, string to, bool overwrite) => File.Move(from, to, overwrite);

   public void Delete(string file)
   {
      if(File.Exists(file)) File.Delete(file); //File.Delete throws when the file's folder is gone, which counts as no such file here.
   }
}
