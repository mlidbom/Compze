using Compze.Contracts;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Teventive.StateStore.Storage;
using Compze.Threading.ResourceAccess;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.StateStore._private.Files;

///<summary>One unit of work's write to a state store kept in files, prepared: each changed taggregate's file written whole under a<br/>
/// temporary name in the store's own folder, waiting for the commit to rename it, and the files to delete. What can fail, serializing<br/>
/// and writing, has happened when the write is prepared.</summary>
sealed class PreparedFileWrite : IPreparedWrite
{
   readonly IFileSystem _files;
   readonly string _storesFolder;
   readonly string _ownFolder;
   readonly IThreadShared<StoredFiles> _storedFiles;
   readonly List<(Guid Id, string TemporaryFile, string File)> _saved = [];
   readonly List<(Guid? Id, string File)> _deleted = [];

   internal PreparedFileWrite(IFileSystem files, string storesFolder, string ownFolder, IThreadShared<StoredFiles> storedFiles)
   {
      _files = files;
      _storesFolder = storesFolder;
      _ownFolder = ownFolder;
      _storedFiles = storedFiles;
   }

   ///<summary>Writes <paramref name="content"/> under a temporary name, to become <paramref name="file"/>, the file of the taggregate<br/>
   /// <paramref name="id"/>, at the commit. The taggregate's old file is deleted then, if it had another.</summary>
   internal void Save(Guid id, string file, string content)
   {
      State.Assert(_saved.None(it => string.Equals(it.File, file, StringComparison.OrdinalIgnoreCase)),
                   () => $"Two taggregates of one unit of work have the same file, {file}: {_saved.First(it => string.Equals(it.File, file, StringComparison.OrdinalIgnoreCase)).Id} and {id}.");
      var (oldFile, taggregateInTheFile) = _storedFiles.Locked(it => (it.FileOf(id), it.TaggregateIn(file)));
      State.Assert(taggregateInTheFile == null || taggregateInTheFile == id, () => $"Taggregate {id} has the file of taggregate {taggregateInTheFile}, {file}.");

      _files.CreateFolder(_ownFolder);
      _files.CreateFolder(Path.GetDirectoryName(file)!);
      var temporaryFile = Path.Combine(_ownFolder, $"{Guid.NewGuid():N}{CommitJournal.TemporaryFileExtension}");
      _saved.Add((id, temporaryFile, file));
      _files.WriteAllText(temporaryFile, content);
      if(oldFile != null && !string.Equals(oldFile, file, StringComparison.OrdinalIgnoreCase)) _deleted.Add((Id: null, oldFile));
   }

   ///<summary>Deletes the file of the taggregate <paramref name="id"/> at the commit.</summary>
   internal void Delete(Guid id)
   {
      var file = _storedFiles.Locked(it => it.FileOf(id));
      State.Assert(file != null, () => $"Taggregate {id} is to be deleted, and the store knows no file of it.");
      _deleted.Add((id, file));
   }

   bool WritesNothing => _saved.Count == 0 && _deleted.Count == 0;

   ///<summary>Renames the temporary files and deletes the files to delete, as one: the journal it writes first is the commit.</summary>
   ///<remarks>The unit of work's transaction is committing, and can no longer fail. So a failure here, before the journal is written<br/>
   /// or after, stops the process: the next start either finishes the write from the journal, or deletes the temporary files of a<br/>
   /// commit that has no journal.</remarks>
   public void Commit()
   {
      if(WritesNothing) return;
      try
      {
         new CommitJournal(_files, _storesFolder, _saved.Select(it => (it.TemporaryFile, it.File)), _deleted.Select(it => it.File)).WriteAndCarryOut(_ownFolder);
         _storedFiles.Locked(it =>
         {
            foreach(var saved in _saved) it.Record(saved.Id, saved.File);
            foreach(var deleted in _deleted) if(deleted.Id is {} id) it.Forget(id);
         });
      }
#pragma warning disable CA1031 //A failure while committing is surfaced by stopping the process: the transaction can no longer fail.
      catch(Exception exception)
#pragma warning restore CA1031
      {
         Environment.FailFast($"A state store kept in files failed to commit a unit of work's files in {_storesFolder}. The process stops so that the next start can finish the write from its journal, or discard it if no journal was written.", exception);
      }
   }

   ///<summary>Deletes the temporary files: the unit of work rolled back.</summary>
   public void Discard()
   {
      foreach(var saved in _saved) _files.Delete(saved.TemporaryFile);
   }
}
