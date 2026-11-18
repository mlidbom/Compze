using Compze.Contracts;
using Compze.Teventive.StateStore._private.Storage;
using Compze.Threading.ResourceAccess;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.StateStore._private.Files;

///<summary>One write to a state store kept in files, prepared: each taggregate's file written whole under a temporary name in the<br/>
/// store's own folder, waiting for the commit to rename it, and the files to delete. What can fail, serializing and writing, has<br/>
/// happened when the write is prepared.</summary>
///<remarks>A unit of work's write commits in one of two ways. By default the files take part in the transaction as its one durable<br/>
/// participant, <see cref="FilesAsADurableParticipant"/>, and the write commits as the transaction's decision, before the unit of<br/>
/// work commits: <see cref="CommitAsTheTransactionsDecision"/>. Where the consumer has allowed the files in transactions that also<br/>
/// write a database (<see cref="TaggregateFilesBuilder.AcceptingNonAtomicCommitsWithADatabase"/>), it commits when the unit of work<br/>
/// commits: <see cref="Commit"/>. A write of every taggregate anew (<see cref="IStateStore.WriteAllToStorage"/>) commits outside<br/>
/// any transaction: <see cref="CommitOutsideATransaction"/>.</remarks>
sealed class PreparedFileWrite : IPreparedWrite
{
   readonly IFileSystem _files;
   readonly string _storesFolder;
   readonly string _ownFolder;
   readonly IThreadShared<StoredFiles> _storedFiles;
   readonly List<SavedFile> _saved = [];
   readonly List<(Guid Id, string File)> _deleted = [];
   bool _commitsAsTheTransactionsDecision;
   bool _hasCommitted;

   internal PreparedFileWrite(IFileSystem files, string storesFolder, string ownFolder, IThreadShared<StoredFiles> storedFiles)
   {
      _files = files;
      _storesFolder = storesFolder;
      _ownFolder = ownFolder;
      _storedFiles = storedFiles;
   }

   ///<summary>Writes <paramref name="content"/> under a temporary name, to become <paramref name="file"/>, the file of the taggregate<br/>
   /// <paramref name="id"/>, at the commit.</summary>
   internal void Save(Guid id, string file, string content)
   {
      _files.CreateFolder(_ownFolder);
      _files.CreateFolder(Path.GetDirectoryName(file)!);
      var saved = new SavedFile(id, Path.Combine(_ownFolder, $"{Guid.NewGuid():N}{CommitJournal.TemporaryFileExtension}"), file);
      _saved.Add(saved);
      _files.WriteAllText(saved.TemporaryFile, content);
      saved.Stamp = _files.StampOf(saved.TemporaryFile);
   }

   ///<summary>Deletes the file of the taggregate <paramref name="id"/> at the commit.</summary>
   internal void Delete(Guid id)
   {
      var file = _storedFiles.Locked(it => it.FileOf(id));
      State.Assert(file != null, () => $"Taggregate {id} is to be deleted, and the store knows no file of it.");
      _deleted.Add((id, file));
   }

   bool WritesNothing => _saved.Count == 0 && _deleted.Count == 0;

   ///<summary>Has the write commit as its transaction's decision, <see cref="CommitAsTheTransactionsDecision"/>, called by the files'<br/>
   /// durable participant, instead of when the unit of work commits.</summary>
   internal void CommitsAsTheTransactionsDecision() => _commitsAsTheTransactionsDecision = true;

   ///<summary>The unit of work commits: commits the write, unless it committed as its transaction's decision, which comes first.</summary>
   ///<remarks>The unit of work's transaction is committing, and can no longer fail. So a failure here, before the journal is written<br/>
   /// or after, stops the process: the next start either finishes the write from the journal, or deletes the temporary files of a<br/>
   /// commit that has no journal.</remarks>
   public void Commit()
   {
      if(_commitsAsTheTransactionsDecision)
      {
         State.Assert(_hasCommitted, () => $"The write of a state store kept in files, in {_storesFolder}, committed as its transaction's decision, and yet hadn't when the unit of work committed.");
         return;
      }

      if(WritesNothing) return;
      try
      {
         Journal().WriteAndCarryOut(_ownFolder);
         RecordTheCommit();
      }
#pragma warning disable CA1031 //A failure while committing is surfaced by stopping the process: the transaction can no longer fail.
      catch(Exception exception)
#pragma warning restore CA1031
      {
         StopTheProcess(exception);
      }
   }

   ///<summary>Commits the write as its transaction's decision: <see cref="WriteTheJournalThenCarryItOut"/>.</summary>
   internal void CommitAsTheTransactionsDecision()
   {
      WriteTheJournalThenCarryItOut();
      _hasCommitted = true;
   }

   ///<summary>Commits the write by itself, with no transaction to decide: <see cref="WriteTheJournalThenCarryItOut"/>.</summary>
   internal void CommitOutsideATransaction()
   {
      if(WritesNothing) return;
      WriteTheJournalThenCarryItOut();
   }

   ///<summary>Writes the journal, which is the commit, then carries it out.</summary>
   ///<remarks>A failure before the journal is written deletes the temporary files and throws, so nothing is written. A failure after<br/>
   /// it stops the process, as in <see cref="Commit"/>: the journal is written, so the write has happened, and only the next start can<br/>
   /// finish it.</remarks>
   void WriteTheJournalThenCarryItOut()
   {
      string journalFile;
      var journal = Journal();
      try
      {
         journalFile = journal.Write(_ownFolder);
      }
      catch
      {
         Discard();
         throw;
      }

      try
      {
         journal.CarryOutAndDelete(journalFile);
         RecordTheCommit();
      }
#pragma warning disable CA1031 //A failure after the journal is written is surfaced by stopping the process: the write has happened, and only the next start can finish it.
      catch(Exception exception)
#pragma warning restore CA1031
      {
         StopTheProcess(exception);
      }
   }

   CommitJournal Journal() => new(_files, _storesFolder, _saved.Select(it => (it.TemporaryFile, it.File)), _deleted.Select(it => it.File));

   ///<summary>Records each file the commit wrote, with the length and last write time its temporary file had, which renaming keeps,<br/>
   /// and forgets each it deleted.</summary>
   void RecordTheCommit() =>
      _storedFiles.Locked(it =>
      {
         foreach(var saved in _saved) it.Record(saved.Id, saved.File, saved.Stamp._assert().NotNull());
         foreach(var deleted in _deleted) it.Forget(deleted.Id);
      });

   void StopTheProcess(Exception exception) =>
      Environment.FailFast($"A state store kept in files failed to commit a write of its files in {_storesFolder}. The process stops so that the next start can finish the write from its journal, or discard it if no journal was written.", exception);

   ///<summary>Deletes the temporary files: the unit of work rolled back, or the write failed before its journal was written.</summary>
   public void Discard()
   {
      foreach(var saved in _saved) _files.Delete(saved.TemporaryFile);
   }

   ///<summary>A taggregate's file, written under a temporary name until the commit renames it.</summary>
   sealed class SavedFile(Guid id, string temporaryFile, string file)
   {
      internal Guid Id { get; } = id;
      internal string TemporaryFile { get; } = temporaryFile;
      internal string File { get; } = file;

      ///<summary>The temporary file's length and last write time once it is written, which the file keeps when it is renamed.</summary>
      internal FileStamp? Stamp { get; set; }
   }
}
