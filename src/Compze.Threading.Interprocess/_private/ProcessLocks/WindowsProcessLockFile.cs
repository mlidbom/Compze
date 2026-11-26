using System.Text;

namespace Compze.Threading.Interprocess._private.ProcessLocks;

///<summary>A process lock's file on Windows. Its holder has it open for writing and shares it only for reading, which keeps out every other holder,<br/>
/// as each opens it the same way, but not a virus scanner or an indexer that opens it to read. The holder writes in the file itself what it holds<br/>
/// the lock for, and those who wait read it there.</summary>
sealed class WindowsProcessLockFile : ProcessLockFile
{
   ///<summary>The <see cref="Exception.HResult"/> of the <see cref="IOException"/> for a file that another handle has open in a way that keeps ours<br/>
   /// out: <c>ERROR_SHARING_VIOLATION</c>.</summary>
   const int SharingViolation = unchecked((int)0x80070020);

   internal WindowsProcessLockFile(string lockFilePath) : base(lockFilePath) {}

   internal override IProcessLockHold? TryHold(string holderDescription)
   {
      var lockFile = TryOpenAsItsHolder();
      try
      {
         if(lockFile == null) return null;

         lockFile.SetLength(0);
         lockFile.Write(Encoding.UTF8.GetBytes(holderDescription));
         lockFile.Flush();
         var hold = new ProcessLockFileHold(lockFile);
         lockFile = null; //The hold has it now: what follows must not close it.
         return hold;
      }
      finally
      {
         //Unless the hold has it: a description that couldn't be written must not leave the lock held with no hold to release it.
         lockFile?.Dispose();
      }
   }

   ///<summary>Opens the file for writing, sharing it only for reading: or returns null if another holder has it open so.</summary>
   FileStream? TryOpenAsItsHolder()
   {
      try
      {
         return new FileStream(LockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
      }
      catch(IOException exception) when(exception.HResult == SharingViolation)
      {
         return null;
      }
   }

   ///<remarks>Shares the file for writing as it reads it: its holder has it open for writing.</remarks>
   internal override string? HolderDescription()
   {
      using var reader = new StreamReader(new FileStream(LockFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
      return reader.ReadToEnd() is { Length: > 0 } holderDescription ? holderDescription : null;
   }
}
