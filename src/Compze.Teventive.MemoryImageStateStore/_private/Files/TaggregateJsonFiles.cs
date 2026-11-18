using Compze.Abstractions;
using Compze.Contracts;
using Compze.Teventive.MemoryImageStateStore._internal;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.MemoryImageStateStore._private.Files;

///<summary>The layout of a memory image state store's JSON files: the store's folder, the files of each class in it, and the store's own<br/>
/// folder. The storage writes and reads by it, and the container resolves it as <see cref="ITaggregateJsonFiles"/>, one for each<br/>
/// container.</summary>
sealed class TaggregateJsonFiles : ITaggregateJsonFiles
{
   ///<summary>The store's own folder under its folder, for the journals of its commits, its temporary files and the caches of a<br/>
   /// <see cref="ITaggregateFilesLoadCache"/>: no class's folder can be in it.</summary>
   internal const string OwnFolderName = ".state-store";

   readonly IReadOnlyDictionary<Type, FilesOfAClass> _classes;

   ///<summary>The layout of the store in <paramref name="folder"/>, whose classes' files are as <paramref name="declarations"/> declare.</summary>
   internal TaggregateJsonFiles(DirectoryInfo folder, IEnumerable<FilesOfAClass.Declaration> declarations)
   {
      Folder = Path.GetFullPath(folder.FullName);
      OwnFolder = Path.Combine(Folder, OwnFolderName);
      _classes = declarations.Select(it => it.In(Folder)).ToDictionary(it => it.TaggregateClass);
   }

   ///<summary>The full path of the store's folder.</summary>
   internal string Folder { get; }

   ///<summary>The full path of the store's own folder, <see cref="OwnFolderName"/> under its folder.</summary>
   internal string OwnFolder { get; }

   internal IReadOnlyCollection<FilesOfAClass> Classes => [.._classes.Values];

   ///<summary>The files of <paramref name="taggregateClass"/>, which the store keeps.</summary>
   internal FilesOfAClass Of(Type taggregateClass) => _classes[taggregateClass];

   public string PathOf<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate => PathOf(typeof(TTaggregate), id);

   public string PathOf(ITaggregate taggregate) => PathOf(taggregate.GetType(), taggregate.Id);

   ///<summary>The full path of the file of the <paramref name="taggregateClass"/> <paramref name="id"/>. Throws, naming<br/>
   /// <paramref name="taggregateClass"/> and the classes the store keeps, if the store keeps no <paramref name="taggregateClass"/>.</summary>
   string PathOf(Type taggregateClass, TaggregateId id)
   {
      Argument.Assert(_classes.ContainsKey(taggregateClass),
                      () => $"The memory image state store keeps no {taggregateClass.FullName}, so it has no file of one: it keeps {string.Join(", ", _classes.Keys.Select(it => it.FullName))}.");
      return Of(taggregateClass).FileOf(id);
   }

   public TaggregateOfAFile? TaggregateWhoseFileIs(string path)
   {
      Argument.Assert(Path.IsPathFullyQualified(path),
                      () => $"\"{path}\" is not a full path. Combine a path relative to a folder, such as one version control gives relative to a checkout, with the folder first.");
      var fullPath = Path.GetFullPath(path);
      return _classes.Values.Select(it => it.TaggregateWhoseFileIs(fullPath)).OfType<TaggregateOfAFile>().SingleOrDefault();
   }
}
