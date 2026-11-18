using System.Text.Json;
using System.Transactions;
using Compze.Abstractions;
using Compze.Contracts;
using Compze.Teventive.MemoryImageStateStore._internal;
using Compze.Teventive.MemoryImageStateStore._private.Storage;
using Compze.Teventive.Taggregates._internal;
using Compze.Threading.ResourceAccess;

namespace Compze.Teventive.MemoryImageStateStore._private.Files;

///<summary>Compze's own storage for a memory image state store: a JSON file for each taggregate, in a folder for each class, each file holding<br/>
/// exactly what the consumer's <see cref="JsonSerializerOptions"/> make of the taggregate's snapshot. A commit writes all its files or<br/>
/// none: see <see cref="CommitJournal"/>.</summary>
///<remarks>Composed with <see cref="MemoryImageStateStoreBuilder.StoredInJsonFiles(DirectoryInfo,JsonSerializerOptions,Action{TaggregateFilesBuilder})"/>.<br/>
/// Where each file is, is <see cref="TaggregateJsonFiles"/>. The store's own files, the journals, the temporary files and the caches<br/>
/// of a <see cref="ITaggregateFilesLoadCache"/>, are in <see cref="TaggregateJsonFiles.OwnFolderName"/> under the store's folder, on<br/>
/// the same volume as the taggregates' files, so renaming a temporary file moves no data.</remarks>
sealed class TaggregateFileStorage : ITaggregateStorage
{
   const string LoadCacheExtension = ".load-cache";

   readonly TaggregateJsonFiles _jsonFiles;
   readonly JsonSerializerOptions _options;
   readonly IReadOnlyDictionary<Type, SnapshotableTaggregateClass> _classes;
   readonly ITaggregateFilesLoadCache? _loadCache;
   readonly IFileSystem _files;
   readonly bool _isAcceptingNonAtomicCommitsWithADatabase;
   readonly IThreadShared<StoredFiles> _storedFiles = IThreadShared.New(new StoredFiles());

   ///<summary>The storage of <paramref name="classes"/> in files laid out as <paramref name="jsonFiles"/> says, on <paramref name="files"/>:<br/>
   /// the disk, or a file system in memory for specifications. <paramref name="isAcceptingNonAtomicCommitsWithADatabase"/>: as<br/>
   /// <see cref="TaggregateFilesBuilder.AcceptingNonAtomicCommitsWithADatabase"/> declares.</summary>
   internal TaggregateFileStorage(TaggregateJsonFiles jsonFiles, JsonSerializerOptions options, IEnumerable<SnapshotableTaggregateClass> classes, ITaggregateFilesLoadCache? loadCache, IFileSystem files, bool isAcceptingNonAtomicCommitsWithADatabase)
   {
      _jsonFiles = jsonFiles;
      _files = files;
      _isAcceptingNonAtomicCommitsWithADatabase = isAcceptingNonAtomicCommitsWithADatabase;
      _options = options;
      _classes = classes.ToDictionary(it => it.Class);
      _loadCache = loadCache;
   }

   ///<summary>Finishes any commit a crash interrupted, then loads every file of every class: from the load cache where it holds the<br/>
   /// file unchanged, by reading the file otherwise.</summary>
   public IReadOnlyList<TaggregateSnapshot> LoadAll()
   {
      CommitJournal.FinishInterruptedCommits(_files, _jsonFiles.Folder, _jsonFiles.OwnFolder);
      var loaded = _jsonFiles.Classes.SelectMany(LoadTheFilesOf).ToList();
      _storedFiles.Locked(it => it.ReplaceWith(loaded.Select(load => (load.Taggregate!.Id.Value, load.File))));
      return [..loaded.Select(it => it.Snapshot)];
   }

   ///<summary>Whether <paramref name="loaded"/> holds the state <paramref name="heldSnapshot"/> holds, as this storage writes state: if<br/>
   /// the text its file holds is what the store would write for <paramref name="heldSnapshot"/>, or else if <paramref name="loaded"/>'s<br/>
   /// snapshot serializes to that text too, the file holding the same state written otherwise, such as indented differently.</summary>
   public bool HoldTheSameState(TaggregateSnapshot loaded, object heldSnapshot)
   {
      var held = Serialized(loaded.TaggregateClass, heldSnapshot);
      return loaded.TextReadFrom == held || Serialized(loaded.TaggregateClass, loaded.Snapshot) == held;
   }

   ///<summary>What <see cref="_options"/> make of <paramref name="snapshot"/>, a snapshot of a <paramref name="taggregateClass"/>: what<br/>
   /// the taggregate's file holds.</summary>
   string Serialized(Type taggregateClass, object snapshot) => JsonSerializer.Serialize(snapshot, _classes[taggregateClass].SnapshotType, _options);

   IReadOnlyList<LoadedFile> LoadTheFilesOf(FilesOfAClass files)
   {
      var taggregateClass = _classes[files.TaggregateClass];
      if(_loadCache == null) return [..files.Files(_files).Select(file => Load(taggregateClass, file, cachedSnapshot: null))];

      var cacheFile = LoadCacheFileOf(files);
      var cached = OpenTheLoadCache(cacheFile, taggregateClass);
      var loaded = files.Files(_files).Select(file => Load(taggregateClass, file, cached.SnapshotOf(file))).ToList();
      if(cached.ContentAfter([..loaded.Select(it => new LoadedSnapshot(it.File, it.Snapshot.Snapshot))]) is {} content) WriteWhole(cacheFile, content);
      return loaded;
   }

   ICachedFilesOfAClass OpenTheLoadCache(string cacheFile, SnapshotableTaggregateClass taggregateClass)
   {
      try
      {
         return _loadCache!.Open(_files.FileExists(cacheFile) ? _files.ReadAllBytes(cacheFile) : null, taggregateClass.SnapshotType);
      }
      catch(Exception exception)
      {
         throw new InvalidOperationException($"The memory image state store can't read its load cache {cacheFile}: {exception.Message} Delete it, and the store starts by reading every file.", exception);
      }
   }

   ///<summary>The file the load cache keeps of <paramref name="files"/>' class in the store's own folder, named by the class's folder.</summary>
   string LoadCacheFileOf(FilesOfAClass files) =>
      Path.Combine(_jsonFiles.OwnFolder, Uri.EscapeDataString(files.RelativeFolder.Replace(Path.DirectorySeparatorChar, '/')) + LoadCacheExtension);

   ///<summary>Writes <paramref name="content"/> to <paramref name="file"/> whole: under a temporary name first, then renamed, so a crash<br/>
   /// leaves the file as it was or as it is to be. The start after a crash deletes the temporary file, as it deletes a commit's.</summary>
   void WriteWhole(string file, byte[] content)
   {
      var temporaryFile = file + CommitJournal.TemporaryFileExtension;
      _files.CreateFolder(Path.GetDirectoryName(file)!);
      _files.WriteAllBytes(temporaryFile, content);
      CommitJournal.RetriedWhileAnotherProgramHoldsTheFile(() => _files.Move(temporaryFile, file, overwrite: true));
   }

   ///<remarks>A write that writes or deletes a file takes part in the unit of work's transaction as its one durable participant, before<br/>
   /// anything is written, unless the files accept commits that aren't atomic with a database's: see<br/>
   /// <see cref="FilesAsADurableParticipant"/>.</remarks>
   public IPreparedWrite Prepare(IReadOnlyList<TaggregateSnapshot> toSave, IReadOnlyList<TaggregateId> toDelete)
   {
      var write = new PreparedFileWrite(_files, _jsonFiles.Folder, _jsonFiles.OwnFolder, _storedFiles);
      if(!_isAcceptingNonAtomicCommitsWithADatabase && (toSave.Count > 0 || toDelete.Count > 0))
         FilesAsADurableParticipant.TakePartIn(Transaction.Current._assert().NotNull(), write, _jsonFiles.Folder);
      Write(write, toSave, toDelete);
      return write;
   }

   ///<summary>Writes every file of <paramref name="everything"/> under a temporary name, then commits them as one, through the journal,<br/>
   /// with no transaction: a failure before the journal is written writes nothing, and comes out of this call.</summary>
   public void WriteAll(IReadOnlyList<TaggregateSnapshot> everything)
   {
      var write = new PreparedFileWrite(_files, _jsonFiles.Folder, _jsonFiles.OwnFolder, _storedFiles);
      Write(write, everything, toDelete: []);
      write.CommitOutsideATransaction();
   }

   ///<summary>Prepares <paramref name="write"/>: writes the file of each of <paramref name="toSave"/> under a temporary name, and notes<br/>
   /// each of <paramref name="toDelete"/> to delete. Discards what it wrote if any of it fails.</summary>
   void Write(PreparedFileWrite write, IReadOnlyList<TaggregateSnapshot> toSave, IReadOnlyList<TaggregateId> toDelete)
   {
      try
      {
         foreach(var saved in toSave) write.Save(saved.Id.Value, _jsonFiles.Of(saved.TaggregateClass).FileOf(saved.Id), Serialized(saved.TaggregateClass, saved.Snapshot));
         foreach(var deleted in toDelete) write.Delete(deleted.Value);
      }
      catch
      {
         write.Discard();
         throw;
      }
   }

   ///<summary><paramref name="file"/>'s snapshot, <paramref name="cachedSnapshot"/> where the load cache held it unchanged, else read<br/>
   /// from the file as a snapshot of <paramref name="taggregateClass"/>, whose folder it is in, and the taggregate restored from it,<br/>
   /// which tells its id. Whatever fails is rethrown naming the file.</summary>
   ///<remarks>The snapshot tells whether the file is as the store last read or wrote it, and, where it isn't and the store held the<br/>
   /// taggregate, the text read from it, for a reload to compare. It keeps no other text, so a load holds no file's text it needn't.</remarks>
   LoadedFile Load(SnapshotableTaggregateClass taggregateClass, ListedFile file, object? cachedSnapshot)
   {
      try
      {
         var text = cachedSnapshot == null ? _files.ReadAllText(file.FullPath) : null;
         var snapshot = cachedSnapshot
                     ?? JsonSerializer.Deserialize(text!, taggregateClass.SnapshotType, _options)
                     ?? throw new JsonException("The file holds null.");
         var taggregate = taggregateClass.Restore(snapshot);
         var (isAsLastReadOrWritten, theStoreHeldIt) = _storedFiles.Locked(it => (it.IsAsLastReadOrWritten(taggregate.Id.Value, file), it.Holds(taggregate.Id.Value)));
         return new LoadedFile(file,
                               new TaggregateSnapshot(taggregateClass.Class, taggregate.Id, snapshot)
                               {
                                  RestoredTaggregate = taggregate,
                                  IsUnchangedSinceTheStoreLastReadOrWroteIt = isAsLastReadOrWritten,
                                  TextReadFrom = !isAsLastReadOrWritten && theStoreHeldIt ? text : null
                               });
      }
      catch(Exception exception)
      {
         throw new InvalidOperationException($"The memory image state store can't load {file.FullPath} as a {taggregateClass.Class.Name}: {exception.Message}", exception);
      }
   }

   sealed class LoadedFile(ListedFile file, TaggregateSnapshot snapshot)
   {
      internal ListedFile File { get; } = file;
      internal TaggregateSnapshot Snapshot { get; } = snapshot;
      internal ITaggregate? Taggregate => Snapshot.RestoredTaggregate;
   }
}
