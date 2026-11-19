namespace Compze.Teventive.MemoryImageStateStore.Testing;

///<summary>The file system in memory of a memory image state store whose files are on one (<see cref="TaggregateFilesBuilderTestingCE.OnAFileSystemInMemory"/>),<br/>
/// for a specification: to read what the store wrote, and to change the files behind the store's back, as another program changing<br/>
/// them on the disk would, such as to specify what a reload does.</summary>
///<remarks>Resolvable from the container when the store's files are on a file system in memory. Each path is a full path, under the<br/>
/// folder the store was composed with, as <see cref="ITaggregateJsonFiles.PathOf{TTaggregate}"/> gives a taggregate's, and paths<br/>
/// are compared ignoring case, as on Windows. A folder exists only as the paths of the files in it. The files written before the<br/>
/// store's first use are what it loads.</remarks>
public interface IFileSystemInMemory
{
   ///<summary>The full path of every file, the store's own files in its <c>.state-store</c> folder included, in no particular order.</summary>
   IReadOnlyCollection<string> Paths { get; }

   ///<summary>The text of the file <paramref name="path"/>, as <see cref="File.ReadAllText(string)"/> reads a file on the disk. Throws<br/>
   /// <see cref="FileNotFoundException"/> if there is no such file.</summary>
   string ReadAllText(string path);

   ///<summary>Writes <paramref name="content"/> to the file <paramref name="path"/> in UTF-8 without a byte order mark, replacing the<br/>
   /// file if there is one, as <see cref="File.WriteAllText(string,string)"/> writes on the disk. The file gets a last write time later<br/>
   /// than any before, so a cache of the store's start sees it changed.</summary>
   void WriteAllText(string path, string content);

   ///<summary>Deletes the file <paramref name="path"/>, and does nothing if there is no such file, as <see cref="File.Delete"/> does.</summary>
   void Delete(string path);
}
