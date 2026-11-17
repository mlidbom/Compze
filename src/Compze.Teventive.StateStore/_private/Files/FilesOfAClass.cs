using Compze.Contracts;
using Compze.Teventive.Taggregates._internal;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.StateStore._private.Files;

///<summary>Where a state store kept in files keeps the taggregates of one class: the class's folder, and the file of each taggregate<br/>
/// in it. Also how its files are read: as the class's snapshot type.</summary>
sealed class FilesOfAClass
{
   const string FileExtension = ".json";
   readonly Func<ITaggregate, DirectoryInfo, FileInfo> _fileOf;

   FilesOfAClass(SnapshotableTaggregateClass taggregateClass, DirectoryInfo folder, Func<ITaggregate, DirectoryInfo, FileInfo> fileOf)
   {
      TaggregateClass = taggregateClass;
      Folder = folder;
      _fileOf = fileOf;
   }

   internal SnapshotableTaggregateClass TaggregateClass { get; }

   DirectoryInfo Folder { get; }

   ///<summary>The full path of <paramref name="taggregate"/>'s file. Asserts that it is under the class's folder and ends in .json.</summary>
   internal string FileOf(ITaggregate taggregate)
   {
      var file = Path.GetFullPath(_fileOf(taggregate, Folder).FullName);
      State.Assert(IsUnderTheFolder(file) && file.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase),
                   () => $"The file declared for {TaggregateClass.Class.Name} {taggregate.Id}, {file}, isn't a {FileExtension} file under the folder of the class's files, {Folder.FullName}.");
      return file;
   }

   ///<summary>Every file of the class: each .json file under its folder, at any depth, as the disk holds them now.</summary>
   ///<remarks>Asks the disk by the folder's path: a <see cref="DirectoryInfo"/> keeps what it first learned of the folder, so the<br/>
   /// folder a commit created after the store's first load would stay missing to it.</remarks>
   internal IEnumerable<string> Files() =>
      Directory.Exists(Folder.FullName) ? Directory.EnumerateFiles(Folder.FullName, $"*{FileExtension}", SearchOption.AllDirectories) : [];

   bool IsUnderTheFolder(string file) => file.StartsWith(Path.TrimEndingDirectorySeparator(Folder.FullName) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

   ///<summary>The file of a taggregate when its class declares none: <c>&lt;first two hex digits of the id&gt;/&lt;id&gt;.json</c>, so that<br/>
   /// no folder holds more than a few thousand files of a store with hundreds of thousands.</summary>
   static FileInfo InBucketsById(ITaggregate taggregate, DirectoryInfo folder)
   {
      var id = taggregate.Id.Value;
      return new FileInfo(Path.Combine(folder.FullName, id.ToString("N")[..2], $"{id}{FileExtension}"));
   }

   ///<summary>The files of one class as <see cref="TaggregateFiles.Of{TTaggregate}"/> declares them, before the store's folder is known.</summary>
   internal sealed class Declaration
   {
      const string StoresOwnFolderName = TaggregateFileStorage.OwnFolderName;
      readonly Func<ITaggregate, DirectoryInfo, FileInfo>? _fileOf;

      internal Declaration(Type taggregateClass, string folder, Func<ITaggregate, DirectoryInfo, FileInfo>? fileOf)
      {
         Argument.Assert(IsAFolderUnderTheStoresFolder(folder),
                         () => $"The folder of {taggregateClass.FullName}'s files, \"{folder}\", must be a relative path to a folder under the store's folder, and not {StoresOwnFolderName}, the store's own.");
         TaggregateClass = taggregateClass;
         Folder = Path.TrimEndingDirectorySeparator(folder.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
         _fileOf = fileOf;
      }

      internal Type TaggregateClass { get; }

      ///<summary>The folder, relative to the store's folder, with the platform's separators.</summary>
      internal string Folder { get; }

      ///<summary>Whether <paramref name="other"/>'s folder is this one's, or one is inside the other: either way loading one class's<br/>
      /// folder would read the other's files.</summary>
      internal bool SharesFilesWith(Declaration other) => IsTheSameOrInside(Folder, other.Folder) || IsTheSameOrInside(other.Folder, Folder);

      internal FilesOfAClass For(SnapshotableTaggregateClass taggregateClass, DirectoryInfo storesFolder) =>
         new(taggregateClass, new DirectoryInfo(Path.Combine(storesFolder.FullName, Folder)), _fileOf ?? InBucketsById);

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
