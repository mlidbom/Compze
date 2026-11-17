using System.Text.Json;
using Compze.Teventive.StateStore.Storage;
using Compze.Threading.ResourceAccess;

namespace Compze.Teventive.StateStore._private.Files;

///<summary>Compze's own storage for a state store: a JSON file for each taggregate, in a folder for each class, each file holding<br/>
/// exactly what the consumer's <see cref="JsonSerializerOptions"/> make of the taggregate's snapshot. A commit writes all its files or<br/>
/// none: see <see cref="CommitJournal"/>.</summary>
///<remarks>Composed with <see cref="StateStoreBuilder.StoredInFiles(DirectoryInfo,JsonSerializerOptions,Action{TaggregateFiles})"/>.<br/>
/// The store's own files, the journals and the temporary files, are in <see cref="OwnFolderName"/> under the store's folder, on the<br/>
/// same volume as the taggregates' files, so renaming a temporary file moves no data.</remarks>
sealed class TaggregateFileStorage : ITaggregateStorage
{
   internal const string OwnFolderName = ".state-store";

   readonly string _folder;
   readonly string _ownFolder;
   readonly JsonSerializerOptions _options;
   readonly IReadOnlyDictionary<Type, FilesOfAClass> _classes;
   readonly IThreadShared<StoredFiles> _storedFiles = IThreadShared.New(new StoredFiles());

   internal TaggregateFileStorage(DirectoryInfo folder, JsonSerializerOptions options, IEnumerable<FilesOfAClass> classes)
   {
      _folder = Path.GetFullPath(folder.FullName);
      _ownFolder = Path.Combine(_folder, OwnFolderName);
      _options = options;
      _classes = classes.ToDictionary(it => it.TaggregateClass.Class);
   }

   ///<summary>Finishes any commit a crash interrupted, then reads every file of every class.</summary>
   public IReadOnlyList<TaggregateSnapshot> LoadAll()
   {
      CommitJournal.FinishInterruptedCommits(_folder, _ownFolder);
      var loaded = _classes.Values.SelectMany(files => files.Files().Select(file => Load(files, file))).ToList();
      _storedFiles.Locked(it => it.ReplaceWith(loaded.Select(load => (load.Taggregate!.Id.Value, load.File))));
      return [..loaded.Select(it => it.Snapshot)];
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

   ///<summary><paramref name="file"/>, read as a snapshot of the class <paramref name="files"/> are of, and the taggregate restored from it,<br/>
   /// which tells its id. Whatever fails is rethrown naming the file.</summary>
   LoadedFile Load(FilesOfAClass files, string file)
   {
      try
      {
         var snapshot = JsonSerializer.Deserialize(File.ReadAllText(file), files.TaggregateClass.SnapshotType, _options)
                     ?? throw new JsonException("The file holds null.");
         var taggregate = files.TaggregateClass.Restore(snapshot);
         return new LoadedFile(file, new TaggregateSnapshot(files.TaggregateClass.Class, taggregate.Id, snapshot) { RestoredTaggregate = taggregate });
      }
      catch(Exception exception)
      {
         throw new InvalidOperationException($"The state store can't load {file} as a {files.TaggregateClass.Class.Name}: {exception.Message}", exception);
      }
   }

   sealed class LoadedFile(string file, TaggregateSnapshot snapshot)
   {
      internal string File { get; } = file;
      internal TaggregateSnapshot Snapshot { get; } = snapshot;
      internal ITaggregate? Taggregate => Snapshot.RestoredTaggregate;
   }
}
