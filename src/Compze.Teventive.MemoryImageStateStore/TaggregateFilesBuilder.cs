using Compze.Contracts;
using Compze.Teventive.MemoryImageStateStore._internal;
using Compze.Teventive.MemoryImageStateStore._private.Files;
using Compze.Teventive.MemoryImageStateStore.Testing;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.MemoryImageStateStore;

///<summary>Declares, for a memory image state store kept in files (<see cref="MemoryImageStateStoreBuilder.StoredInJsonFiles(DirectoryInfo,System.Text.Json.JsonSerializerOptions,Action{TaggregateFilesBuilder})"/>),<br/>
/// the folder each taggregate class's files are in, and the options that apply to all of them.</summary>
///<remarks>Write the options for all the files first, before the folders, so that none reads as an option of the class declared<br/>
/// before it: <c>files => files.AcceptingNonAtomicCommitsWithADatabase().FolderOf&lt;VocabNote&gt;("vocab").FolderOf&lt;KanjiNote&gt;("kanji")</c>.<br/>
/// They are <see cref="AcceptingNonAtomicCommitsWithADatabase"/>, the file system in memory for specifications in the<br/>
/// Compze.Teventive.MemoryImageStateStore.Testing namespace (<see cref="TaggregateFilesBuilderTestingCE.OnAFileSystemInMemory"/>), and a<br/>
/// cache a package of its own plugs in, such as Compze.Teventive.MemoryImageStateStore.MemoryPack's <c>StartingFromAMemoryPackCache()</c>.</remarks>
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

   ///<summary>Puts the files on a file system in memory: what <see cref="TaggregateFilesBuilderTestingCE.OnAFileSystemInMemory"/> declares.</summary>
   internal TaggregateFilesBuilder PutOnAFileSystemInMemory()
   {
      State.Assert(!IsOnAFileSystemInMemory, () => "That the files are on a file system in memory is declared twice.");
      IsOnAFileSystemInMemory = true;
      return this;
   }

   ///<summary>Whether <see cref="TaggregateFilesBuilderTestingCE.OnAFileSystemInMemory"/> is declared.</summary>
   internal bool IsOnAFileSystemInMemory { get; private set; }

   ///<summary>Accepts that a unit of work that writes the files and a database doesn't commit the two atomically: a crash between<br/>
   /// their commits loses the files' change, or, in an endpoint with exactly-once delivery, delivers a tevent about a change that was<br/>
   /// lost, or counts a tessage as handled while its change was lost. Without it such a unit of work is refused: see<br/>
   /// <see cref="MemoryImageStateStoreBuilder.StoredInJsonFiles(DirectoryInfo,System.Text.Json.JsonSerializerOptions,Action{TaggregateFilesBuilder})"/>.</summary>
   ///<remarks>The files then commit when the unit of work commits, which is after the database has committed, whichever the database: each<br/>
   /// of Compze's connections commits as its transaction's decision. A failure committing them stops the process<br/>
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
