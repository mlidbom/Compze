using System.Security.Cryptography;
using System.Text;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading.Interprocess._internal;

namespace Compze.Teventive.MemoryImageStateStore._private.Tables;

///<summary>The signal of one database's memory image state store tables, shared by every process of this machine's user: a memory image<br/>
/// raises it once a commit of its own to the tables has committed, and one that catches up as others commit waits for it, so that it<br/>
/// catches up within about 50 milliseconds of another's commit, and queries the database only when one has.</summary>
///<remarks>A hint, never what the store's correctness rests on: a memory image catches up at each unit of work's first change of the<br/>
/// store, and a get for update takes the taggregate's state from its row. It is named from the database's identity, so two spellings of<br/>
/// one database's connection string get two signals.<br/>
/// It is <c>InterprocessSignal</c>, of Compze.Threading.Interprocess: a counter in a memory-mapped file, raised by an interlocked<br/>
/// increment and awaited by polling a memory read, backing off to at most 50 milliseconds between reads.</remarks>
sealed class CommitSignal : IDisposable
{
   static readonly DirectoryInfo Folder = new(Path.Combine(Path.GetTempPath(), "Compze", "memory-image-state-store-commit-signals"));

   readonly InterprocessSignal _signal;

   ///<summary>The signal of the database <paramref name="databaseIdentity"/> names.</summary>
   internal CommitSignal(string databaseIdentity)
   {
      Folder.Create();
      _signal = new InterprocessSignal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(databaseIdentity))), Folder);
   }

   ///<summary>Tells the memory images waiting for the signal that a commit to the tables has committed.</summary>
   internal void Raise() => _signal.Raise();

   ///<summary>How many times the signal has been raised: what <see cref="AwaitARaiseAfterAsync"/> waits for a change from.</summary>
   internal long RaisedSoFar() => _signal.Snapshot();

   ///<summary>Waits until the signal is raised after it had been raised <paramref name="raisedSoFar"/> times, or<br/>
   /// <paramref name="cancellation"/> is cancelled.</summary>
   internal async Task AwaitARaiseAfterAsync(long raisedSoFar, CancellationToken cancellation)
   {
      var waitStartedAt = DateTime.UtcNow;
      while(!await _signal.TryAwaitChangeFromAsync(raisedSoFar, TimeSpan.FromMinutes(1), waitStartedAt, cancellation).caf()) cancellation.ThrowIfCancellationRequested();
   }

   public void Dispose() => _signal.Dispose();
}
