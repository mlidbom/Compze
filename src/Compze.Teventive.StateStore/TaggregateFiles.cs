using Compze.Contracts;
using Compze.Teventive.StateStore._internal;
using Compze.Teventive.StateStore._private.Files;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.StateStore;

///<summary>Declares, for a state store kept in files (<see cref="StateStoreBuilder.StoredInFiles(DirectoryInfo,System.Text.Json.JsonSerializerOptions,Action{TaggregateFiles})"/>),<br/>
/// the folder each taggregate class's files are in, and the file of each taggregate.</summary>
///<remarks>review-naming: TaggregateFiles, a placeholder for the naming pass (jastudio-rep-opus's fork, 2026-11-17).</remarks>
public sealed class TaggregateFiles
{
   readonly Dictionary<Type, FilesOfAClass.Declaration> _declarations = new();

   internal TaggregateFiles() {}

   ///<summary>Declares that the files of <typeparamref name="TTaggregate"/> are in <paramref name="folder"/>, a folder under the store's<br/>
   /// folder, and that the file of each is the one <paramref name="fileOf"/> gives, from the taggregate and the class's folder.<br/>
   /// Without <paramref name="fileOf"/>, the file of each is <c>&lt;first two hex digits of the id&gt;/&lt;id&gt;.json</c>.</summary>
   ///<remarks>The file of a taggregate may depend on more than its id: when a change gives it another file, the commit writes the<br/>
   /// new file and deletes the old one. The file must be under the class's folder, and end in <c>.json</c>, and no two taggregates may<br/>
   /// have the same file: a unit of work that breaks one of these fails.<br/>
   /// review-naming: Of and fileOf, placeholders for the naming pass (jastudio-rep-opus's fork, 2026-11-17).</remarks>
   public TaggregateFiles Of<TTaggregate>(string folder, Func<TTaggregate, DirectoryInfo, FileInfo>? fileOf = null) where TTaggregate : class, ITaggregate
   {
      State.Assert(!_declarations.ContainsKey(typeof(TTaggregate)), () => $"The files of {typeof(TTaggregate).FullName} are declared twice.");
      _declarations.Add(typeof(TTaggregate),
                        new FilesOfAClass.Declaration(typeof(TTaggregate),
                                                      folder,
                                                      fileOf == null ? null : (taggregate, classFolder) => fileOf((TTaggregate)taggregate, classFolder)));
      return this;
   }

   internal IReadOnlyCollection<FilesOfAClass.Declaration> Declarations => _declarations.Values;

   ///<summary>The cache the store starts from, if a package of its own plugged one in, such as Compze.Teventive.StateStore.MemoryPack.</summary>
   internal ITaggregateFilesLoadCache? LoadCache { get; private set; }

   ///<summary>Has the store start from <paramref name="cache"/>, reading only the files that changed since it was written.</summary>
   internal TaggregateFiles StartFrom(ITaggregateFilesLoadCache cache)
   {
      State.Assert(LoadCache == null, () => "A cache for the store's start is declared twice.");
      LoadCache = cache;
      return this;
   }
}
