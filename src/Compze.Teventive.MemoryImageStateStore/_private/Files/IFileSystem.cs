using Compze.Teventive.MemoryImageStateStore._internal;

namespace Compze.Teventive.MemoryImageStateStore._private.Files;

///<summary>The file operations a memory image state store kept in files makes: on the disk (<see cref="FilesOnDisk"/>), or on a file system in memory<br/>
/// for specifications (<see cref="FileSystemInMemory"/>). Every path is a full path.</summary>
///<remarks>Each operation does what the <see cref="File"/> or <see cref="Directory"/> method of the same name does on the disk, so the<br/>
/// store works the same on either.</remarks>
interface IFileSystem
{
   bool FileExists(string file);

   bool FolderExists(string folder);

   ///<summary>Creates <paramref name="folder"/>, and each folder above it that doesn't exist yet.</summary>
   void CreateFolder(string folder);

   ///<summary>The files in <paramref name="folder"/>, and in the folders under it when <paramref name="includingSubfolders"/>, each with<br/>
   /// its length and last write time, which listing the folder gives without opening the files, and its path relative to<br/>
   /// <paramref name="folder"/>.</summary>
   IReadOnlyList<ListedFile> FilesIn(string folder, bool includingSubfolders);

   ///<summary>The length and last write time of <paramref name="file"/>, as listing its folder gives them.</summary>
   FileStamp StampOf(string file);

   string ReadAllText(string file);

   byte[] ReadAllBytes(string file);

   ///<summary>Writes <paramref name="content"/> in UTF-8 without a byte order mark, replacing the file if there is one.</summary>
   void WriteAllText(string file, string content);

   void WriteAllBytes(string file, byte[] content);

   ///<summary>Renames <paramref name="from"/> to <paramref name="to"/>, as one operation, replacing <paramref name="to"/> if<br/>
   /// <paramref name="overwrite"/>, and failing if it exists otherwise.</summary>
   void Move(string from, string to, bool overwrite);

   ///<summary>Deletes <paramref name="file"/>, and does nothing if there is no such file.</summary>
   void Delete(string file);
}
