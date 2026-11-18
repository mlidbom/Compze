using Compze.Abstractions;
using Compze.Contracts;
using Compze.Teventive.MemoryImageStateStore._internal;
using Compze.Teventive.Taggregates._internal;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.MemoryImageStateStore._private.Files;

///<summary>Where a memory image state store kept in files keeps the taggregates of one class: the class's folder, and the file of each taggregate<br/>
/// in it, which its id gives. Also how its files are read: as the class's snapshot type.</summary>
sealed class FilesOfAClass
{
   const string FileExtension = ".json";

   FilesOfAClass(SnapshotableTaggregateClass taggregateClass, DirectoryInfo folder, string relativeFolder)
   {
      TaggregateClass = taggregateClass;
      Folder = folder;
      RelativeFolder = relativeFolder;
   }

   internal SnapshotableTaggregateClass TaggregateClass { get; }

   DirectoryInfo Folder { get; }

   ///<summary>The class's folder, relative to the store's folder, as it is declared.</summary>
   internal string RelativeFolder { get; }

   ///<summary>The full path of the file of the taggregate <paramref name="id"/>: <c>&lt;first two hex digits of the id&gt;/&lt;id&gt;.json</c><br/>
   /// under the class's folder, so that no folder holds more than a few thousand files of a store with hundreds of thousands.</summary>
   internal string FileOf(TaggregateId id) => Path.GetFullPath(Path.Combine(Folder.FullName, id.Value.ToString("N")[..2], $"{id.Value}{FileExtension}"));

   ///<summary>Every file of the class: each .json file under its folder, at any depth, as <paramref name="files"/> hold them now, with<br/>
   /// the length and last write time that listing the folder gives without opening the files.</summary>
   ///<remarks>Asks by the folder's path: a <see cref="DirectoryInfo"/> keeps what it first learned of the folder, so the folder a<br/>
   /// commit created after the store's first load would stay missing to it.</remarks>
   internal IReadOnlyList<ListedFile> Files(IFileSystem files) =>
      files.FolderExists(Folder.FullName)
         ? [..files.FilesIn(Folder.FullName, includingSubfolders: true).Where(it => it.FullPath.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))]
         : [];

   ///<summary>The files of one class as <see cref="TaggregateFilesBuilder.FolderOf{TTaggregate}"/> declares them, before the store's folder is known.</summary>
   internal sealed class Declaration
   {
      const string StoresOwnFolderName = TaggregateFileStorage.OwnFolderName;

      internal Declaration(Type taggregateClass, string folder)
      {
         Argument.Assert(IsAFolderUnderTheStoresFolder(folder),
                         () => $"The folder of {taggregateClass.FullName}'s files, \"{folder}\", must be a relative path to a folder under the store's folder, and not {StoresOwnFolderName}, the store's own.");
         TaggregateClass = taggregateClass;
         Folder = Path.TrimEndingDirectorySeparator(folder.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
      }

      internal Type TaggregateClass { get; }

      ///<summary>The folder, relative to the store's folder, with the platform's separators.</summary>
      internal string Folder { get; }

      ///<summary>Whether <paramref name="other"/>'s folder is this one's, or one is inside the other: either way loading one class's<br/>
      /// folder would read the other's files.</summary>
      internal bool SharesFilesWith(Declaration other) => IsTheSameOrInside(Folder, other.Folder) || IsTheSameOrInside(other.Folder, Folder);

      internal FilesOfAClass For(SnapshotableTaggregateClass taggregateClass, DirectoryInfo storesFolder) =>
         new(taggregateClass, new DirectoryInfo(Path.Combine(storesFolder.FullName, Folder)), Folder);

      static bool IsTheSameOrInside(string folder, string other) =>
         string.Equals(folder, other, StringComparison.OrdinalIgnoreCase) || folder.StartsWith(other + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

      static bool IsAFolderUnderTheStoresFolder(string folder)
      {
         if(string.IsNullOrWhiteSpace(folder) || Path.IsPathRooted(folder)) return false;
         var segments = folder.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
         return segments.Length > 0 && segments.All(it => it is not ("." or "..")) && !string.Equals(segments[0], StoresOwnFolderName, StringComparison.OrdinalIgnoreCase);
      }
   }
}
