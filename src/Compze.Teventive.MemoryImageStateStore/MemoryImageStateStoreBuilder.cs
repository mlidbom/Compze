using System.Reflection;
using System.Text.Json;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Teventive.MemoryImageStateStore._private;
using Compze.Teventive.MemoryImageStateStore._private.Files;
using Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;
using Compze.Teventive.MemoryImageStateStore._private.Storage;
using Compze.Teventive.MemoryImageStateStore._private.Tables;
using Compze.Teventive.MemoryImageStateStore.Testing;
using Compze.Teventive.TaggregateStores._internal;
using Compze.Teventive.Taggregates;
using Compze.Teventive.Taggregates._internal;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.MemoryImageStateStore;

///<summary>Declares, where a memory image state store is composed, the taggregate classes it keeps, its indexes, and where it stores what it commits.<br/>
/// The store is a memory image of taggregates: every one held in memory for the container's life, rebuilt at its start from a snapshot of<br/>
/// each, which the storage declared here keeps.</summary>
public sealed class MemoryImageStateStoreBuilder
{
   readonly IComponentRegistrar _registrar;
   readonly Dictionary<Type, Func<IRootResolver, SnapshotableTaggregateClass>> _taggregateClasses = new();
   readonly Dictionary<Type, Type> _snapshotTypes = new();
   readonly List<Type> _classesResolvedFromTheRoot = [];
   readonly List<Func<TaggregateMemoryImageStateStore, ITaggregateIndexMaintenance>> _indexes = [];
   readonly HashSet<(Type TaggregateClass, Type KeyType)> _indexTypes = [];
   Func<IRootResolver, IReadOnlyList<SnapshotableTaggregateClass>, ITaggregateStorage>? _storage;
   TaggregateFilesBuilder? _files;
   TaggregateRowsBuilder? _rows;

   internal MemoryImageStateStoreBuilder(IComponentRegistrar registrar) => _registrar = registrar;

   ///<summary>Declares that the store keeps the taggregate class that <paramref name="createEmpty"/> builds new, empty instances of,<br/>
   /// for the store to restore from snapshots. The class and its snapshot type are inferred from the instances it builds.</summary>
   ///<remarks>A factory, not a parameterless constructor, so that the taggregates can be handed what they need. To hand them services<br/>
   /// of the container, use <see cref="Taggregate{TTaggregate}(ComponentRegistration{TTaggregate})"/>.</remarks>
   public MemoryImageStateStoreBuilder Taggregate<TTaggregate, TSnapshot>(Func<ISnapshotable<TTaggregate, TSnapshot>> createEmpty)
      where TTaggregate : class, ITaggregate, ISnapshotable<TTaggregate, TSnapshot> =>
      Declare<TTaggregate, TSnapshot>(_ => SnapshotableTaggregateClass.For(createEmpty));

   ///<summary>Declares that the store keeps the taggregate class that <paramref name="registration"/>, a registration of the container,<br/>
   /// builds new, empty instances of: for a class whose instances need services of the container, as many as its constructor takes,<br/>
   /// such as <c>store.Taggregate(TrackedTransient.For&lt;VocabNote&gt;().CreatedBy((Dictionary dictionary, Readings readings) =&gt; new VocabNote(dictionary, readings)))</c>.<br/>
   /// The snapshot type is found from the class's <see cref="ISnapshotable{TSelf,TSnapshot}"/>.</summary>
   ///<remarks>The store registers <paramref name="registration"/>, and resolves a new instance from the container's root each time it<br/>
   /// restores one: each taggregate at the load and at a reload, and each copy a unit of work gets for update. So the services are the<br/>
   /// root's, and must be singletons, since a taggregate outlives every scope. A registration that takes a scoped service, directly or<br/>
   /// through a tracked transient it takes, is refused when the container is built, with a message naming the class and the scoped<br/>
   /// service, the same whichever container Compze builds on. The registration<br/>
   /// must be a <see cref="Lifestyle.TrackedTransient"/>, so that each instance is new, and the class mustn't be <see cref="IDisposable"/>:<br/>
   /// the root disposes a tracked transient only with the container, so it would hold every instance the store built.<br/>
   /// <b>A taggregate's state must be its own.</b> Its services are for what it does with its own state. Reaching other taggregates<br/>
   /// through a service is what Compze doesn't recommend: a unit of work changes a private copy, so what a taggregate computes from<br/>
   /// other taggregates goes stale.</remarks>
   public MemoryImageStateStoreBuilder Taggregate<TTaggregate>(ComponentRegistration<TTaggregate> registration) where TTaggregate : class, ITaggregate
   {
      AssertBuildsInstancesTheStoreCanKeep(registration);
      return (MemoryImageStateStoreBuilder)DeclareRegisteredMethod.MakeGenericMethod(typeof(TTaggregate), SnapshotTypeOf<TTaggregate>())
                                                       .Invoke(this, BindingFlags.DoNotWrapExceptions, binder: null, [registration], culture: null)!;
   }

   static readonly MethodInfo DeclareRegisteredMethod = typeof(MemoryImageStateStoreBuilder).GetMethod(nameof(DeclareRegistered), BindingFlags.NonPublic | BindingFlags.Instance)!;

   MemoryImageStateStoreBuilder DeclareRegistered<TTaggregate, TSnapshot>(ComponentRegistration<TTaggregate> registration)
      where TTaggregate : class, ITaggregate, ISnapshotable<TTaggregate, TSnapshot>
   {
      var declared = Declare<TTaggregate, TSnapshot>(resolver => SnapshotableTaggregateClass.For(resolver.Resolve<TTaggregate>));
      _registrar.Register(registration);
      _classesResolvedFromTheRoot.Add(typeof(TTaggregate));
      return declared;
   }

   ///<summary>The taggregate classes declared with a registration of the container, whose instances the store resolves from the<br/>
   /// container's root, which the store's own registration declares, so that building the container refuses one whose registration<br/>
   /// takes a scoped service.</summary>
   internal IReadOnlyList<Type> ClassesResolvedFromTheRoot => _classesResolvedFromTheRoot;

   static void AssertBuildsInstancesTheStoreCanKeep<TTaggregate>(ComponentRegistration<TTaggregate> registration) where TTaggregate : class
   {
      Argument.Assert(registration.Lifestyle == Lifestyle.TrackedTransient,
                      () => $"{typeof(TTaggregate).FullName} is declared to the memory image state store with a {registration.Lifestyle} registration. The store builds a new instance each time it restores one, so the registration must be a {nameof(Lifestyle.TrackedTransient)}: TrackedTransient.For<{typeof(TTaggregate).Name}>().CreatedBy(...).");
      Argument.Assert(!typeof(IDisposable).IsAssignableFrom(typeof(TTaggregate)),
                      () => $"{typeof(TTaggregate).FullName} is IDisposable. The memory image state store builds it through the container's root, which disposes a tracked transient only with the container, so the container would hold every instance the store built. A taggregate built through the container can't be IDisposable.");
   }

   static Type SnapshotTypeOf<TTaggregate>()
   {
      var snapshotables = typeof(TTaggregate).GetInterfaces()
                                             .Where(it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(ISnapshotable<,>) && it.GenericTypeArguments[0] == typeof(TTaggregate))
                                             .ToList();
      Argument.Assert(snapshotables.Count == 1,
                      () => $"{typeof(TTaggregate).FullName} implements ISnapshotable<{typeof(TTaggregate).Name}, TSnapshot> {snapshotables.Count} times. The memory image state store keeps a class that implements it once, for a snapshot type of its own.");
      return snapshotables[0].GenericTypeArguments[1];
   }

   MemoryImageStateStoreBuilder Declare<TTaggregate, TSnapshot>(Func<IRootResolver, SnapshotableTaggregateClass> declare)
   {
      State.Assert(!_taggregateClasses.ContainsKey(typeof(TTaggregate)), () => $"{typeof(TTaggregate).FullName} is declared twice for the memory image state store.");
      _taggregateClasses.Add(typeof(TTaggregate), declare);
      _snapshotTypes.Add(typeof(TTaggregate), typeof(TSnapshot));
      return this;
   }

   ///<summary>Declares an <see cref="ITaggregateIndex{TTaggregate,TKey}"/>, which files each <typeparamref name="TTaggregate"/> under the<br/>
   /// keys <paramref name="keysOf"/> gives from its state. The container then resolves it.</summary>
   ///<remarks>An index is known by its taggregate class and key type, as the container resolves it, so a class has at most one index<br/>
   /// by each key type: two indexes of one class need key types of their own. Declaring a second throws.<br/>
   /// <paramref name="keysOf"/> gives keys from the taggregate's own state alone, the same each time for the same state, and changes<br/>
   /// nothing, since it runs on instances other threads read at the same time. What a lookup costs, and what a lookup during a commit<br/>
   /// can find, is in <see cref="ITaggregateIndex{TTaggregate,TKey}"/>'s documentation.</remarks>
   public MemoryImageStateStoreBuilder Index<TTaggregate, TKey>(Func<TTaggregate, IEnumerable<TKey>> keysOf) where TTaggregate : class, ITaggregate where TKey : notnull
   {
      State.Assert(_indexTypes.Add((typeof(TTaggregate), typeof(TKey))),
                   () => $"The memory image state store already has an index of {typeof(TTaggregate).FullName} by {typeof(TKey).FullName}, and an index is known by its taggregate class and key type alone. Give each index a key type of its own.");
      _indexes.Add(store => new TaggregateIndex<TTaggregate, TKey>(store, keysOf));
      _registrar.Register(Singleton.For<ITaggregateIndex<TTaggregate, TKey>>().CreatedBy((TaggregateMemoryImageStateStore store) => store.Index<TTaggregate, TKey>()));
      return this;
   }

   ///<summary>Declares that the store keeps what it commits in JSON files, in <paramref name="folder"/>: a file for each taggregate, in a<br/>
   /// folder for each class, which <paramref name="declare"/> declares, holding exactly what <paramref name="options"/> make of the<br/>
   /// taggregate's snapshot. A unit of work's files are written all or none. The container then resolves <see cref="ITaggregateJsonFiles"/>,<br/>
   /// which gives the path of each taggregate's file, and the taggregate each file is the file of.</summary>
   ///<remarks>The store keeps its own files, its journals and temporary files, in <c>.state-store</c> under <paramref name="folder"/>.<br/>
   /// A consumer whose files are in version control ignores that folder.<br/>
   /// <b>A unit of work that writes the files can't also hold a database connection,</b> by default. The files and a database commit<br/>
   /// one after the other, and a crash between their commits would lose the files' change, or, in an endpoint with exactly-once<br/>
   /// delivery, deliver a tevent about a change that was lost, or count a tessage as handled while its change was lost. So such a unit<br/>
   /// of work is refused, and rolls back: with a message naming both when the connection is Compze's, as the tevent store's and the<br/>
   /// exactly-once outbox's and inbox's are; by .NET itself for a SQL Server connection opened outside Compze. A PostgreSQL, MySQL or<br/>
   /// SQLite connection opened outside Compze isn't seen. A unit of work that only reads the store is never refused, and a container<br/>
   /// may hold this store and the tevent store: only one transaction holding both is refused. A consumer to whom that loss is<br/>
   /// acceptable declares <see cref="TaggregateFilesBuilder.AcceptingNonAtomicCommitsWithADatabase"/>.<br/>
   /// The files commit as their transaction's one durable participant, in a single phase, so they never commit as part of a<br/>
   /// distributed transaction. A failure while writing a commit's journal rolls the unit of work back. <b>A failure after the journal<br/>
   /// is written stops the process</b> (<see cref="Environment.FailFast(string)"/>), such as a file another program holds for more<br/>
   /// than about two seconds: the commit has happened, and the next start finishes it from its journal. The files are written, not<br/>
   /// flushed to the disk: a commit outlives the process, not the machine losing its power.</remarks>
   public MemoryImageStateStoreBuilder StoredInJsonFiles(DirectoryInfo folder, JsonSerializerOptions options, Action<TaggregateFilesBuilder> declare) =>
      DeclareFiles(_ => folder, servicesTheFolderIsFoundWith: [], options, declare);

   ///<summary>As <see cref="StoredInJsonFiles(DirectoryInfo,JsonSerializerOptions,Action{TaggregateFilesBuilder})"/>, for a folder that a service of<br/>
   /// the container knows: <c>store.StoredInJsonFiles((CorpusPaths paths) => paths.Corpus, options, files => ...)</c>.</summary>
   ///<remarks>The service is resolved from the container's root when the store is built, at its first use, or when<br/>
   /// <see cref="ITaggregateJsonFiles"/> is first resolved. So it can't be scoped: a <typeparamref name="TService"/> that is scoped, or takes<br/>
   /// a scoped service, directly or through a tracked transient it takes, is refused when the container is built, with a message naming<br/>
   /// the scoped service, the same whichever container Compze builds on.</remarks>
   public MemoryImageStateStoreBuilder StoredInJsonFiles<TService>(Func<TService, DirectoryInfo> folderWith, JsonSerializerOptions options, Action<TaggregateFilesBuilder> declare) where TService : class =>
      DeclareFiles(resolver => folderWith(resolver.Resolve<TService>()), servicesTheFolderIsFoundWith: [typeof(TService)], options, declare);

   ///<summary>Declares the store's JSON files, in the folder <paramref name="folderWith"/> finds with the<br/>
   /// <paramref name="servicesTheFolderIsFoundWith"/>, which it resolves from the container's root: the registration of<br/>
   /// <see cref="ITaggregateJsonFiles"/> declares them, so that building the container refuses a scoped one.</summary>
   MemoryImageStateStoreBuilder DeclareFiles(Func<IRootResolver, DirectoryInfo> folderWith, IReadOnlyList<Type> servicesTheFolderIsFoundWith, JsonSerializerOptions options, Action<TaggregateFilesBuilder> declare)
   {
      var files = new TaggregateFilesBuilder();
      declare(files);
      _files = files;
      DeclareStorage((resolver, classes) => new TaggregateFileStorage(resolver.Resolve<TaggregateJsonFiles>(),
                                                                      options,
                                                                      classes,
                                                                      files.LoadCache,
                                                                      files.IsOnAFileSystemInMemory ? resolver.Resolve<FileSystemInMemory>() : FilesOnDisk.Instance,
                                                                      files.IsAcceptingNonAtomicCommitsWithADatabase));
      _registrar.Register(Singleton.For<ITaggregateJsonFiles, TaggregateJsonFiles>()
                                   .ResolvingFromTheRoot(servicesTheFolderIsFoundWith)
                                   .CreatedBy((IRootResolver resolver) => new TaggregateJsonFiles(folderWith(resolver), files.Declarations)));
      if(files.IsOnAFileSystemInMemory) _registrar.Register(Singleton.For<IFileSystemInMemory, FileSystemInMemory>().CreatedBy(() => new FileSystemInMemory()));
      return this;
   }

   ///<summary>Declares that the store keeps what it commits in tables of the domain database: the database the container declares, such as<br/>
   /// with <c>SqliteDomainDatabase(...)</c>, beside the store's SQL layer for it, such as <c>SqliteMemoryImageStateStoreSqlLayer()</c>,<br/>
   /// of the package Compze.Teventive.MemoryImageStateStore.Sqlite or its siblings for SQL Server, PostgreSQL and MySQL. A row for each<br/>
   /// taggregate holds the name <paramref name="declare"/> declares for its class, and exactly what <paramref name="options"/> make of<br/>
   /// its snapshot. A unit of work commits as one database transaction, which also holds whatever else the unit of work writes to the<br/>
   /// domain database, such as an exactly-once endpoint's inbox and outbox.</summary>
   ///<remarks><b>Several memory images may share the tables:</b> other processes, and other containers of the same process, such as two<br/>
   /// exactly-once endpoints, each with a memory image of its own. Getting a taggregate for update, or adding one, takes the<br/>
   /// taggregate's lock in the database as well as in memory, and takes its state from its row if another memory image has committed<br/>
   /// it since. And a memory image catches up with what the others committed at each unit of work's first get for update or add.<br/>
   /// Reads outside a unit of work read what the memory image last caught up with, with no round trip to the database.</remarks>
   ///<remarks>Each memory image holds every taggregate. Commits that write the store's rows take their numbers from a counter in the<br/>
   /// database, whose lock they hold until they commit, so they commit one at a time while they write their rows. On SQLite, every<br/>
   /// transaction that writes the database runs alone, across processes too, as everything Compze does on SQLite does.</remarks>
   ///<remarks>review-naming: StoredInTheDomainDatabase, beside StoredInJsonFiles (state-store-in-tables, 2026-11-24).</remarks>
   public MemoryImageStateStoreBuilder StoredInTheDomainDatabase(JsonSerializerOptions options, Action<TaggregateRowsBuilder> declare)
   {
      var rows = new TaggregateRowsBuilder();
      declare(rows);
      _rows = rows;
      return DeclareStorage((resolver, classes) => new TaggregateTableStorage(resolver.Resolve<IMemoryImageStateStoreSqlLayer>(), options, new TaggregateClassNames(rows.NamesByClass, classes)));
   }

   MemoryImageStateStoreBuilder DeclareStorage(Func<IRootResolver, IReadOnlyList<SnapshotableTaggregateClass>, ITaggregateStorage> storage)
   {
      State.Assert(_storage == null, () => "The memory image state store's storage is declared twice.");
      _storage = storage;
      return this;
   }

   ///<summary>Asserts, once everything is declared, that the declarations fit together: that a store kept in files declares the files<br/>
   /// of each class it keeps, and of no other, that no two classes share a folder, and that the cache it starts from, if any, can<br/>
   /// hold each class's snapshots.</summary>
   internal void AssertDeclarationsFitTogether()
   {
      AssertEveryClassKeptInTablesIsNamed();
      if(_files == null) return;
      var filesDeclared = _files.Declarations.ToList();
      var keptWithoutFiles = _taggregateClasses.Keys.Where(kept => filesDeclared.None(it => it.TaggregateClass == kept)).Select(it => it.FullName).ToList();
      State.Assert(keptWithoutFiles.None(), () => $"The memory image state store is kept in files, and no files are declared for {string.Join(", ", keptWithoutFiles)}. Declare each class's files where the store is composed.");
      var filesOfClassesNotKept = filesDeclared.Where(it => !_taggregateClasses.ContainsKey(it.TaggregateClass)).Select(it => it.TaggregateClass.FullName).ToList();
      State.Assert(filesOfClassesNotKept.None(), () => $"Files are declared for {string.Join(", ", filesOfClassesNotKept)}, which the memory image state store isn't declared to keep.");
      var sharingAFolder = filesDeclared.SelectMany(one => filesDeclared.Where(other => other != one && one.SharesFilesWith(other)).Select(other => new[] { one, other })).FirstOrDefault();
      State.Assert(sharingAFolder == null,
                   () => $"The files of {sharingAFolder![0].TaggregateClass.FullName}, in \"{sharingAFolder[0].Folder}\", and of {sharingAFolder[1].TaggregateClass.FullName}, in \"{sharingAFolder[1].Folder}\", share a folder. Each class's files need a folder of their own.");
      if(_files.LoadCache is {} cache)
         foreach(var snapshotType in _snapshotTypes.Values) cache.AssertCanHold(snapshotType);
   }

   ///<summary>Asserts, for a store kept in tables, that each class it keeps is named, and no class it doesn't keep.</summary>
   void AssertEveryClassKeptInTablesIsNamed()
   {
      if(_rows == null) return;
      var keptWithoutAName = _taggregateClasses.Keys.Where(kept => !_rows.NamesByClass.ContainsKey(kept)).Select(it => it.FullName).ToList();
      State.Assert(keptWithoutAName.None(), () => $"The memory image state store is kept in tables, and no name in the rows is declared for {string.Join(", ", keptWithoutAName)}. Declare each class's name where the store is composed: rows => rows.ClassNamed<TTaggregate>(name).");
      var namedButNotKept = _rows.NamesByClass.Keys.Where(it => !_taggregateClasses.ContainsKey(it)).Select(it => it.FullName).ToList();
      State.Assert(namedButNotKept.None(), () => $"Names in the rows are declared for {string.Join(", ", namedButNotKept)}, which the memory image state store isn't declared to keep.");
   }

   internal TaggregateMemoryImageStateStore Build(TaggregateUnitsOfWork unitsOfWork, IRootResolver resolver)
   {
      var classes = _taggregateClasses.Values.Select(declare => declare(resolver)).ToList();
      var storage = _storage?.Invoke(resolver, classes) ?? NoStorage.Instance;
      var catchingUpOn = storage is TaggregateTableStorage tables && _rows is { IsCatchingUpAsOtherMemoryImagesOnThisMachineCommit: true } ? tables.CommitSignal : null;
      return new(unitsOfWork, classes, _indexes, storage, catchingUpOn);
   }
}
