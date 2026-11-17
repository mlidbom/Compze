using System.Text.Json;
using Compze.Teventive.StateStore.Storage;
using Compze.Teventive.StateStore._internal;
using Compze.Threading.ResourceAccess;

namespace Compze.Teventive.StateStore._private.Files;

///<summary>Compze's own storage for a state store: a JSON file for each taggregate, in a folder for each class, each file holding<br/>
/// exactly what the consumer's <see cref="JsonSerializerOptions"/> make of the taggregate's snapshot. A commit writes all its files or<br/>
/// none: see <see cref="CommitJournal"/>.</summary>
///<remarks>Composed with <see cref="StateStoreBuilder.StoredInFiles(DirectoryInfo,JsonSerializerOptions,Action{TaggregateFiles})"/>.<br/>
/// The store's own files, the journals, the temporary files and the caches of a <see cref="ITaggregateFilesLoadCache"/>, are in<br/>
/// <see cref="OwnFolderName"/> under the store's folder, on the same volume as the taggregates' files, so renaming a temporary file<br/>
/// moves no data.</remarks>
sealed class TaggregateFileStorage : ITaggregateStorage
{
   internal const string OwnFolderName = ".state-store";
   const string LoadCacheExtension = ".load-cache";

   readonly string _folder;
   readonly string _ownFolder;
   readonly JsonSerializerOptions _options;
   readonly IReadOnlyDictionary<Type, FilesOfAClass> _classes;
   readonly ITaggregateFilesLoadCache? _loadCache;
   readonly IThreadShared<StoredFiles> _storedFiles = IThreadShared.New(new StoredFiles());

   internal TaggregateFileStorage(DirectoryInfo folder, JsonSerializerOptions options, IEnumerable<FilesOfAClass> classes, ITaggregateFilesLoadCache? loadCache)
   {
      _folder = Path.GetFullPath(folder.FullName);
      _ownFolder = Path.Combine(_folder, OwnFolderName);
      _options = options;
      _classes = classes.ToDictionary(it => it.TaggregateClass.Class);
      _loadCache = loadCache;
   }

   ///<summary>Finishes any commit a crash interrupted, then loads every file of every class: from the load cache where it holds the<br/>
   /// file unchanged, by reading the file otherwise.</summary>
   public IReadOnlyList<TaggregateSnapshot> LoadAll()
   {
      CommitJournal.FinishInterruptedCommits(_folder, _ownFolder);
      var loaded = _classes.Values.SelectMany(LoadTheFilesOf).ToList();
      _storedFiles.Locked(it => it.ReplaceWith(loaded.Select(load => (load.Taggregate!.Id.Value, load.File.FullPath))));
      return [..loaded.Select(it => it.Snapshot)];
   }

   IReadOnlyList<LoadedFile> LoadTheFilesOf(FilesOfAClass files)
   {
      if(_loadCache == null) return [..files.Files().Select(file => Load(files, file, cachedSnapshot: null))];

      var cacheFile = LoadCacheFileOf(files);
      var cached = OpenTheLoadCache(cacheFile, files);
      var loaded = files.Files().Select(file => Load(files, file, cached.SnapshotOf(file))).ToList();
      if(cached.ContentAfter([..loaded.Select(it => new LoadedSnapshot(it.File, it.Snapshot.Snapshot))]) is {} content) WriteWhole(cacheFile, content);
      return loaded;
   }

   ICachedFilesOfAClass OpenTheLoadCache(string cacheFile, FilesOfAClass files)
   {
      try
      {
         return _loadCache!.Open(File.Exists(cacheFile) ? File.ReadAllBytes(cacheFile) : null, files.TaggregateClass.SnapshotType);
      }
      catch(Exception exception)
      {
         throw new InvalidOperationException($"The state store can't read its load cache {cacheFile}: {exception.Message} Delete it, and the store starts by reading every file.", exception);
      }
   }

   ///<summary>The file the load cache keeps of <paramref name="files"/>' class in the store's own folder, named by the class's folder.</summary>
   string LoadCacheFileOf(FilesOfAClass files) =>
      Path.Combine(_ownFolder, Uri.EscapeDataString(files.RelativeFolder.Replace(Path.DirectorySeparatorChar, '/')) + LoadCacheExtension);

   ///<summary>Writes <paramref name="content"/> to <paramref name="file"/> whole: under a temporary name first, then renamed, so a crash<br/>
   /// leaves the file as it was or as it is to be. The start after a crash deletes the temporary file, as it deletes a commit's.</summary>
   static void WriteWhole(string file, byte[] content)
   {
      var temporaryFile = file + CommitJournal.TemporaryFileExtension;
      Directory.CreateDirectory(Path.GetDirectoryName(file)!);
      File.WriteAllBytes(temporaryFile, content);
      CommitJournal.RetriedWhileAnotherProgramHoldsTheFile(() => File.Move(temporaryFile, file, overwrite: true));
   }

   public IPreparedWrite Prepare(IReadOnlyList<TaggregateSnapshot> toSave, IReadOnlyList<TaggregateToDelete> toDelete)
   {
      var write = new PreparedFileWrite(_folder, _ownFolder, _storedFiles);
      try
      {
         foreach(var saved in toSave)
            write.Save(saved.Id.Value, _classes[saved.TaggregateClass].FileOf(saved.TaggregateToWrite!), JsonSerializer.Serialize(saved.Snapshot, _classes[saved.TaggregateClass].TaggregateClass.SnapshotType, _options));
         foreach(var deleted in toDelete) write.Delete(deleted.Id.Value);
         return write;
      }
      catch
      {
         write.Discard();
         throw;
      }
   }

   ///<summary><paramref name="file"/>'s snapshot, <paramref name="cachedSnapshot"/> where the load cache held it unchanged, else read<br/>
   /// from the file as a snapshot of the class <paramref name="files"/> are of, and the taggregate restored from it, which tells its<br/>
   /// id. Whatever fails is rethrown naming the file.</summary>
   LoadedFile Load(FilesOfAClass files, ListedFile file, object? cachedSnapshot)
   {
      try
      {
         var snapshot = cachedSnapshot
                     ?? JsonSerializer.Deserialize(File.ReadAllText(file.FullPath), files.TaggregateClass.SnapshotType, _options)
                     ?? throw new JsonException("The file holds null.");
         var taggregate = files.TaggregateClass.Restore(snapshot);
         return new LoadedFile(file, new TaggregateSnapshot(files.TaggregateClass.Class, taggregate.Id, snapshot) { RestoredTaggregate = taggregate });
      }
      catch(Exception exception)
      {
         throw new InvalidOperationException($"The state store can't load {file.FullPath} as a {files.TaggregateClass.Class.Name}: {exception.Message}", exception);
      }
   }

   sealed class LoadedFile(ListedFile file, TaggregateSnapshot snapshot)
   {
      internal ListedFile File { get; } = file;
      internal TaggregateSnapshot Snapshot { get; } = snapshot;
      internal ITaggregate? Taggregate => Snapshot.RestoredTaggregate;
   }
}
