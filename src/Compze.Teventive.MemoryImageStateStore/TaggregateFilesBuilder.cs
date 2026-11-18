using Compze.Contracts;
using Compze.Teventive.MemoryImageStateStore._internal;
using Compze.Teventive.MemoryImageStateStore._private.Files;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.MemoryImageStateStore;

///<summary>Declares, for a memory image state store kept in files (<see cref="MemoryImageStateStoreBuilder.StoredInJsonFiles(DirectoryInfo,System.Text.Json.JsonSerializerOptions,Action{TaggregateFilesBuilder})"/>),<br/>
/// the folder each taggregate class's files are in, and the options that apply to all of them.</summary>
///<remarks>Write the options for all the files first, before the folders, so that none reads as an option of the class declared<br/>
/// before it: <c>files => files.KeptInMemory().FolderOf&lt;VocabNote&gt;("vocab").FolderOf&lt;KanjiNote&gt;("kanji")</c>. They are<br/>
/// <see cref="KeptInMemory"/>, <see cref="AcceptingNonAtomicCommitsWithADatabase"/>, and a cache a package of its own plugs in,<br/>
/// such as Compze.Teventive.MemoryImageStateStore.MemoryPack's <c>StartingFromAMemoryPackCache()</c>.</remarks>
public sealed class TaggregateFilesBuilder
{
   readonly Dictionary<Type, FilesOfAClass.Declaration> _declarations = new();

   internal TaggregateFilesBuilder() {}

   ///<summary>Declares that the files of <typeparamref name="TTaggregate"/> are in <paramref name="folder"/>, a folder under the store's<br/>
   /// folder. The file of each is <c>&lt;first two hex digits of the id&gt;/&lt;id&gt;.json</c> in it, for the taggregate's whole life.</summary>
   public TaggregateFilesBuilder FolderOf<TTaggregate>(string folder) where TTaggregate : class, ITaggregate
   {
      State.Assert(!_declarations.ContainsKey(typeof(TTaggregate)), () => $"The files of {typeof(TTaggregate).FullName} are declared twice.");
      _declarations.Add(typeof(TTaggregate), new FilesOfAClass.Declaration(typeof(TTaggregate), folder));
      return this;
   }

   ///<summary>Keeps the files in memory instead of on the disk, for specifications: the store works as it does on the disk, with the<br/>
   /// same layout, the consumer's JSON options, the journal and any cache, and touches no file on the disk. The folder the store is<br/>
   /// composed with is then only where the files' paths begin. A specification reads and changes the files through<br/>
   /// <see cref="ITaggregateFilesInMemory"/>, which the container then resolves.</summary>
   ///<remarks>The files live as long as the container, one set of them for each container: a second container is a second store with<br/>
   /// files of its own, not a store started again on the same files.<br/>
   /// review-naming: KeptInMemory, a placeholder for the naming pass (jastudio-rep-opus's fork, 2026-11-17).</remarks>
   public TaggregateFilesBuilder KeptInMemory()
   {
      State.Assert(!IsKeptInMemory, () => "That the files are kept in memory is declared twice.");
      IsKeptInMemory = true;
      return this;
   }

   ///<summary>Whether <see cref="KeptInMemory"/> is declared.</summary>
   internal bool IsKeptInMemory { get; private set; }

   ///<summary>Accepts that a unit of work that writes the files and a database doesn't commit the two atomically: a crash between<br/>
   /// their commits loses the files' change, or, in an endpoint with exactly-once delivery, delivers a tevent about a change that was<br/>
   /// lost, or counts a tessage as handled while its change was lost. Without it such a unit of work is refused: see<br/>
   /// <see cref="MemoryImageStateStoreBuilder.StoredInJsonFiles(DirectoryInfo,System.Text.Json.JsonSerializerOptions,Action{TaggregateFilesBuilder})"/>.</summary>
   ///<remarks>The files then take part in each transaction as a volatile participant: they commit after a SQL Server connection, and in<br/>
   /// the order they joined with the others, and a failure committing them stops the process<br/>
   /// (<see cref="Environment.FailFast(string)"/>), since the transaction can no longer roll back by then.</remarks>
   public TaggregateFilesBuilder AcceptingNonAtomicCommitsWithADatabase()
   {
      State.Assert(!IsAcceptingNonAtomicCommitsWithADatabase, () => "That the files accept commits that aren't atomic with a database's is declared twice.");
      IsAcceptingNonAtomicCommitsWithADatabase = true;
      return this;
   }

   ///<summary>Whether <see cref="AcceptingNonAtomicCommitsWithADatabase"/> is declared.</summary>
   internal bool IsAcceptingNonAtomicCommitsWithADatabase { get; private set; }

   internal IReadOnlyCollection<FilesOfAClass.Declaration> Declarations => _declarations.Values;

   ///<summary>The cache the store starts from, if a package of its own plugged one in, such as Compze.Teventive.MemoryImageStateStore.MemoryPack.</summary>
   internal ITaggregateFilesLoadCache? LoadCache { get; private set; }

   ///<summary>Has the store start from <paramref name="cache"/>, reading only the files that changed since it was written.</summary>
   internal TaggregateFilesBuilder StartFrom(ITaggregateFilesLoadCache cache)
   {
      State.Assert(LoadCache == null, () => "A cache for the store's start is declared twice.");
      LoadCache = cache;
      return this;
   }
}
