using System.Text.Json;
using Compze.Contracts;

namespace Compze.Teventive.StateStore._private.Files;

///<summary>The journal of one commit of a state store kept in files: each temporary file with the file it becomes, and each file to<br/>
/// delete. It is written, whole, before any of them is renamed or deleted, so the journal is the commit: once it exists, the write<br/>
/// has happened, and a start after a crash finishes it.</summary>
///<remarks>The paths are relative to the store's folder, so a journal still works if the folder has moved since the crash.<br/>
/// A journal is first written under a temporary name and then renamed, so a crash while writing it leaves no journal: that<br/>
/// commit didn't happen, and its temporary files are deleted at the next start.</remarks>
sealed class CommitJournal
{
   const string JournalExtension = ".journal";
   const string JournalBeingWrittenExtension = ".journal-being-written";
   internal const string TemporaryFileExtension = ".being-written";
   const int AttemptsAtAFileAnotherProgramHolds = 20;
   static readonly TimeSpan PauseBetweenAttempts = TimeSpan.FromMilliseconds(100);

   readonly string _storesFolder;
   readonly Content _content;

   CommitJournal(string storesFolder, Content content)
   {
      _storesFolder = storesFolder;
      _content = content;
   }

   ///<summary>A journal of renaming each of <paramref name="renames"/>, a temporary file and the file it becomes, and of deleting<br/>
   /// each of <paramref name="deletions"/>, all full paths under <paramref name="storesFolder"/>.</summary>
   internal CommitJournal(string storesFolder, IEnumerable<(string TemporaryFile, string File)> renames, IEnumerable<string> deletions)
      : this(storesFolder,
             new Content
             {
                Renames = [..renames.Select(it => new Rename { From = Relative(storesFolder, it.TemporaryFile), To = Relative(storesFolder, it.File) })],
                Deletions = [..deletions.Select(it => Relative(storesFolder, it))]
             }) {}

   ///<summary>Writes the journal into <paramref name="ownFolder"/>, carries it out, then deletes it.</summary>
   internal void WriteAndCarryOut(string ownFolder)
   {
      var journalBeingWritten = Path.Combine(ownFolder, $"{Guid.NewGuid():N}{JournalBeingWrittenExtension}");
      var journal = Path.ChangeExtension(journalBeingWritten, JournalExtension);
      File.WriteAllText(journalBeingWritten, JsonSerializer.Serialize(_content));
      File.Move(journalBeingWritten, journal);
      CarryOut();
      File.Delete(journal);
   }

   ///<summary>Finishes each commit whose journal a crash left in <paramref name="ownFolder"/>, then deletes the temporary files of<br/>
   /// commits that never wrote one.</summary>
   internal static void FinishInterruptedCommits(string storesFolder, string ownFolder)
   {
      if(!Directory.Exists(ownFolder)) return;
      foreach(var journal in Directory.EnumerateFiles(ownFolder, $"*{JournalExtension}").ToList())
      {
         var content = JsonSerializer.Deserialize<Content>(File.ReadAllText(journal))._assert().NotNull();
         new CommitJournal(storesFolder, content).CarryOut();
         File.Delete(journal);
      }

      foreach(var leftOver in Directory.EnumerateFiles(ownFolder).Where(IsLeftOverByACommitThatNeverHappened).ToList()) File.Delete(leftOver);

      static bool IsLeftOverByACommitThatNeverHappened(string file) =>
         file.EndsWith(TemporaryFileExtension, StringComparison.OrdinalIgnoreCase) || file.EndsWith(JournalBeingWrittenExtension, StringComparison.OrdinalIgnoreCase);
   }

   ///<summary>Renames each temporary file that still exists, and deletes each file to delete that still exists: a crash may have come<br/>
   /// after some of them, so carrying out a journal again does only what is left.</summary>
   void CarryOut()
   {
      foreach(var rename in _content.Renames)
      {
         var from = Full(rename.From);
         if(!File.Exists(from)) continue;
         var to = Full(rename.To);
         Directory.CreateDirectory(Path.GetDirectoryName(to)!);
         RetriedWhileAnotherProgramHoldsTheFile(() => File.Move(from, to, overwrite: true));
      }

      foreach(var deletion in _content.Deletions.Select(Full).Where(File.Exists))
         RetriedWhileAnotherProgramHoldsTheFile(() => File.Delete(deletion));
   }

   string Full(string relative) => Path.GetFullPath(Path.Combine(_storesFolder, relative));

   static string Relative(string storesFolder, string file) => Path.GetRelativePath(storesFolder, file);

   ///<summary>Runs <paramref name="fileOperation"/>, trying again for about two seconds while another program holds the file: on<br/>
   /// Windows a virus scanner, an indexer or an editor can open a file just written.</summary>
   internal static void RetriedWhileAnotherProgramHoldsTheFile(Action fileOperation)
   {
      for(var attempt = 1;; attempt++)
      {
         try
         {
            fileOperation();
            return;
         }
         catch(Exception exception) when(attempt < AttemptsAtAFileAnotherProgramHolds && IsHeldByAnotherProgram(exception))
         {
            Thread.Sleep(PauseBetweenAttempts);
         }
      }

      static bool IsHeldByAnotherProgram(Exception exception) =>
         exception is UnauthorizedAccessException or IOException { HResult: SharingViolation or LockViolation };
   }

   const int SharingViolation = unchecked((int)0x80070020);
   const int LockViolation = unchecked((int)0x80070021);

   ///<summary>What a journal file holds.</summary>
   sealed class Content
   {
      public List<Rename> Renames { get; init; } = [];
      public List<string> Deletions { get; init; } = [];
   }

   ///<summary>A temporary file, and the file it becomes, each relative to the store's folder.</summary>
   sealed class Rename
   {
      public string From { get; init; } = "";
      public string To { get; init; } = "";
   }
}
