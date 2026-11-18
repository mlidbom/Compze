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

   ///<summary>Keeps the files in memory instead of on the disk, for specifications: the store works as it does on the disk, with the<br/>
   /// same layout, the consumer's JSON options, the journal and any cache, and touches no file on the disk. The folder the store is<br/>
   /// composed with is then only where the files' paths begin. A specification reads and changes the files through<br/>
   /// <see cref="ITaggregateFilesInMemory"/>, which the container then resolves.</summary>
   ///<remarks>The files live as long as the container, one set of them for each container: a second container is a second store with<br/>
   /// files of its own, not a store started again on the same files.<br/>
   /// review-naming: KeptInMemory, a placeholder for the naming pass (jastudio-rep-opus's fork, 2026-11-17).</remarks>
   public TaggregateFiles KeptInMemory()
   {
      State.Assert(!IsKeptInMemory, () => "That the files are kept in memory is declared twice.");
      IsKeptInMemory = true;
      return this;
   }

   ///<summary>Whether <see cref="KeptInMemory"/> is declared.</summary>
   internal bool IsKeptInMemory { get; private set; }

   ///<summary>Allows a unit of work that writes the files to hold a database connection too, accepting that a crash between their<br/>
   /// commits loses the files' change, or, in an endpoint with exactly-once delivery, delivers a tevent about a change that was lost,<br/>
   /// or counts a tessage as handled while its change was lost. Without it such a unit of work is refused: see<br/>
   /// <see cref="StateStoreBuilder.StoredInFiles(DirectoryInfo,System.Text.Json.JsonSerializerOptions,Action{TaggregateFiles})"/>.</summary>
   ///<remarks>The files then take part in each transaction as a volatile participant: they commit after a SQL Server connection, and in<br/>
   /// the order they joined with the others, and a failure committing them stops the process<br/>
   /// (<see cref="Environment.FailFast(string)"/>), since the transaction can no longer roll back by then.<br/>
   /// review-naming: AllowedInTransactionsThatAlsoWriteADatabase, a placeholder for the naming pass (jastudio-rep-opus's fork, 2026-11-18).</remarks>
   public TaggregateFiles AllowedInTransactionsThatAlsoWriteADatabase()
   {
      State.Assert(!IsAllowedInTransactionsThatAlsoWriteADatabase, () => "That the files are allowed in transactions that also write a database is declared twice.");
      IsAllowedInTransactionsThatAlsoWriteADatabase = true;
      return this;
   }

   ///<summary>Whether <see cref="AllowedInTransactionsThatAlsoWriteADatabase"/> is declared.</summary>
   internal bool IsAllowedInTransactionsThatAlsoWriteADatabase { get; private set; }

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
