using System.Text.Json;
using Compze.Abstractions;

namespace Compze.Teventive.MemoryImageStateStore;

///<summary>The JSON files of a memory image state store composed with<br/>
/// <see cref="MemoryImageStateStoreBuilder.StoredInJsonFiles(DirectoryInfo,JsonSerializerOptions,Action{TaggregateFilesBuilder})"/>:<br/>
/// the file of each taggregate, and the taggregate each file is the file of. Resolved from the container when the store is stored<br/>
/// in JSON files, on the disk or on a file system in memory.</summary>
///<remarks>The files are the consumer's artifact as much as the store's: JSON, so that they can be read, diffed and kept in version<br/>
/// control. This is how code around them, such as a view of what version control lists as changed, finds its way between files and<br/>
/// taggregates without knowing the layout.<br/>
/// Paths are full paths, as strings, since the files on a file system in memory aren't on the disk, and are compared ignoring case,<br/>
/// as on Windows, as the store compares them.</remarks>
public interface ITaggregateJsonFiles
{
   ///<summary>The full path of the file of the <typeparamref name="TTaggregate"/> <paramref name="id"/>, whether it exists yet or not.<br/>
   /// Throws if the store keeps no <typeparamref name="TTaggregate"/>.</summary>
   string PathOf<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate;

   ///<summary>The taggregate whose file <paramref name="path"/> is, or null when it is no taggregate's file: a path outside the folders<br/>
   /// of the store's classes, the store's own files under <c>.state-store</c>, or anything but a taggregate's .json file.</summary>
   ///<remarks>It reads only the path, not the file, so it answers for a file that is gone, such as one version control lists as<br/>
   /// deleted, and for one the store doesn't hold yet, such as one a checkout added before a load.<br/>
   /// <paramref name="path"/> must be a full path: a relative one throws, rather than being read against the process's current folder.<br/>
   /// Combine a path that version control gives relative to a checkout with the checkout's folder first. Forward slashes are read as<br/>
   /// <see cref="Path.GetFullPath(string)"/> reads them.</remarks>
   TaggregateOfAFile? TaggregateWhoseFileIs(string path);
}
