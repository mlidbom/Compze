using System.Text.Json;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Teventive.StateStore._private;
using Compze.Teventive.StateStore._private.Files;
using Compze.Teventive.StateStore._private.Storage;
using Compze.Teventive.TaggregateStores._internal;
using Compze.Teventive.Taggregates;
using Compze.Teventive.Taggregates._internal;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.StateStore;

///<summary>Declares, where a state store is composed, the taggregate classes it keeps, its indexes, and where it stores what it commits.</summary>
public sealed class StateStoreBuilder
{
   readonly IComponentRegistrar _registrar;
   readonly Dictionary<Type, Func<IRootResolver, SnapshotableTaggregateClass>> _taggregateClasses = new();
   readonly Dictionary<Type, Type> _snapshotTypes = new();
   readonly List<Func<TaggregateStateStore, ITaggregateIndexMaintenance>> _indexes = [];
   readonly HashSet<(Type TaggregateClass, Type KeyType)> _indexTypes = [];
   Func<IRootResolver, IReadOnlyList<SnapshotableTaggregateClass>, ITaggregateStorage>? _storage;
   TaggregateFiles? _files;

   internal StateStoreBuilder(IComponentRegistrar registrar) => _registrar = registrar;

   ///<summary>Declares that the store keeps the taggregate class that <paramref name="createEmpty"/> builds new, empty instances of,<br/>
   /// for the store to restore from snapshots. The class and its snapshot type are inferred from the instances it builds.</summary>
   ///<remarks>A factory, not a parameterless constructor, so that the taggregates can be handed what they need. To hand them a<br/>
   /// service of the container, use <see cref="Taggregate{TTaggregate,TSnapshot,TService}"/>.<br/>
   /// <paramref name="snapshotsAreEqual"/> tells whether two snapshots hold the same state, for
   /// <see cref="IStateStore.ReloadFromStorage"/> to report what changed. Without it, the snapshot type's own Equals does, and must<br/>
   /// be overridden.</remarks>
   public StateStoreBuilder Taggregate<TTaggregate, TSnapshot>(Func<ISnapshotable<TTaggregate, TSnapshot>> createEmpty, Func<TSnapshot, TSnapshot, bool>? snapshotsAreEqual = null)
      where TTaggregate : class, ITaggregate, ISnapshotable<TTaggregate, TSnapshot> =>
      Declare<TTaggregate, TSnapshot>(_ => SnapshotableTaggregateClass.For(createEmpty, snapshotsAreEqual));

   ///<summary>As <see cref="Taggregate{TTaggregate,TSnapshot}"/>, for a factory that builds the empty instances with a service of the<br/>
   /// container, such as the services every note is built with: <c>store.Taggregate((NoteServices services) => new VocabNote(services))</c>.</summary>
   ///<remarks>The service is resolved from the container's root each time an instance is built.</remarks>
   public StateStoreBuilder Taggregate<TTaggregate, TSnapshot, TService>(Func<TService, ISnapshotable<TTaggregate, TSnapshot>> createEmpty, Func<TSnapshot, TSnapshot, bool>? snapshotsAreEqual = null)
      where TTaggregate : class, ITaggregate, ISnapshotable<TTaggregate, TSnapshot> where TService : class =>
      Declare<TTaggregate, TSnapshot>(resolver => SnapshotableTaggregateClass.For(() => createEmpty(resolver.Resolve<TService>()), snapshotsAreEqual));

   StateStoreBuilder Declare<TTaggregate, TSnapshot>(Func<IRootResolver, SnapshotableTaggregateClass> declare)
   {
      State.Assert(!_taggregateClasses.ContainsKey(typeof(TTaggregate)), () => $"{typeof(TTaggregate).FullName} is declared twice for the state store.");
      _taggregateClasses.Add(typeof(TTaggregate), declare);
      _snapshotTypes.Add(typeof(TTaggregate), typeof(TSnapshot));
      return this;
   }

   ///<summary>Declares an <see cref="ITaggregateIndex{TTaggregate,TKey}"/>, which files each <typeparamref name="TTaggregate"/> under the<br/>
   /// keys <paramref name="keysOf"/> gives from its state. The container then resolves it.</summary>
   ///<remarks>An index is known by its taggregate class and key type, as the container resolves it, so a class has at most one index<br/>
   /// by each key type: two indexes of one class need key types of their own.</remarks>
   public StateStoreBuilder Index<TTaggregate, TKey>(Func<TTaggregate, IEnumerable<TKey>> keysOf) where TTaggregate : class, ITaggregate where TKey : notnull
   {
      State.Assert(_indexTypes.Add((typeof(TTaggregate), typeof(TKey))),
                   () => $"The state store already has an index of {typeof(TTaggregate).FullName} by {typeof(TKey).FullName}, and an index is known by its taggregate class and key type alone. Give each index a key type of its own.");
      _indexes.Add(store => new TaggregateIndex<TTaggregate, TKey>(store, keysOf));
      _registrar.Register(Singleton.For<ITaggregateIndex<TTaggregate, TKey>>().CreatedBy((TaggregateStateStore store) => store.Index<TTaggregate, TKey>()));
      return this;
   }

   ///<summary>Declares that the store keeps what it commits in JSON files, in <paramref name="folder"/>: a file for each taggregate, in a<br/>
   /// folder for each class, which <paramref name="declare"/> declares, holding exactly what <paramref name="options"/> make of the<br/>
   /// taggregate's snapshot. A unit of work's files are written all or none.</summary>
   ///<remarks>The store keeps its own files, its journals and temporary files, in <c>.state-store</c> under <paramref name="folder"/>.<br/>
   /// A consumer whose files are in version control ignores that folder.<br/>
   /// review-naming: StoredInFiles, a placeholder for the naming pass (jastudio-rep-opus's fork, 2026-11-17).</remarks>
   public StateStoreBuilder StoredInFiles(DirectoryInfo folder, JsonSerializerOptions options, Action<TaggregateFiles> declare) =>
      DeclareFiles(_ => folder, options, declare);

   ///<summary>As <see cref="StoredInFiles(DirectoryInfo,JsonSerializerOptions,Action{TaggregateFiles})"/>, for a folder that a service of<br/>
   /// the container knows: <c>store.StoredInFiles((CorpusPaths paths) => paths.Corpus, options, files => ...)</c>.</summary>
   ///<remarks>The service is resolved from the container's root when the store is built, at its first use.</remarks>
   public StateStoreBuilder StoredInFiles<TService>(Func<TService, DirectoryInfo> folderWith, JsonSerializerOptions options, Action<TaggregateFiles> declare) where TService : class =>
      DeclareFiles(resolver => folderWith(resolver.Resolve<TService>()), options, declare);

   StateStoreBuilder DeclareFiles(Func<IRootResolver, DirectoryInfo> folderWith, JsonSerializerOptions options, Action<TaggregateFiles> declare)
   {
      var files = new TaggregateFiles();
      declare(files);
      _files = files;
      if(files.IsKeptInMemory) _registrar.Register(Singleton.For<ITaggregateFilesInMemory, FilesInMemory>().CreatedBy(() => new FilesInMemory()));
      return DeclareStorage((resolver, classes) =>
      {
         var folder = folderWith(resolver);
         return new TaggregateFileStorage(folder,
                                          options,
                                          files.Declarations.Select(it => it.For(classes.Single(declared => declared.Class == it.TaggregateClass), folder)),
                                          files.LoadCache,
                                          files.IsKeptInMemory ? resolver.Resolve<FilesInMemory>() : FilesOnDisk.Instance);
      });
   }

   StateStoreBuilder DeclareStorage(Func<IRootResolver, IReadOnlyList<SnapshotableTaggregateClass>, ITaggregateStorage> storage)
   {
      State.Assert(_storage == null, () => "The state store's storage is declared twice.");
      _storage = storage;
      return this;
   }

   ///<summary>Asserts, once everything is declared, that the declarations fit together: that a store kept in files declares the files<br/>
   /// of each class it keeps, and of no other, that no two classes share a folder, and that the cache it starts from, if any, can<br/>
   /// hold each class's snapshots.</summary>
   internal void AssertDeclarationsFitTogether()
   {
      if(_files == null) return;
      var filesDeclared = _files.Declarations.ToList();
      var keptWithoutFiles = _taggregateClasses.Keys.Where(kept => filesDeclared.None(it => it.TaggregateClass == kept)).Select(it => it.FullName).ToList();
      State.Assert(keptWithoutFiles.None(), () => $"The state store is kept in files, and no files are declared for {string.Join(", ", keptWithoutFiles)}. Declare each class's files where the store is composed.");
      var filesOfClassesNotKept = filesDeclared.Where(it => !_taggregateClasses.ContainsKey(it.TaggregateClass)).Select(it => it.TaggregateClass.FullName).ToList();
      State.Assert(filesOfClassesNotKept.None(), () => $"Files are declared for {string.Join(", ", filesOfClassesNotKept)}, which the state store isn't declared to keep.");
      var sharingAFolder = filesDeclared.SelectMany(one => filesDeclared.Where(other => other != one && one.SharesFilesWith(other)).Select(other => new[] { one, other })).FirstOrDefault();
      State.Assert(sharingAFolder == null,
                   () => $"The files of {sharingAFolder![0].TaggregateClass.FullName}, in \"{sharingAFolder[0].Folder}\", and of {sharingAFolder[1].TaggregateClass.FullName}, in \"{sharingAFolder[1].Folder}\", share a folder. Each class's files need a folder of their own.");
      if(_files.LoadCache is {} cache)
         foreach(var snapshotType in _snapshotTypes.Values) cache.AssertCanHold(snapshotType);
   }

   internal TaggregateStateStore Build(TaggregateUnitsOfWork unitsOfWork, IRootResolver resolver)
   {
      var classes = _taggregateClasses.Values.Select(declare => declare(resolver)).ToList();
      return new(unitsOfWork, classes, _indexes, _storage?.Invoke(resolver, classes) ?? new NoStorage());
   }
}
