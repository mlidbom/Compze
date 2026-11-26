namespace Compze.Threading.Interprocess;

///<summary>A lock that one holder at a time can take, whichever process on the machine it is in: every <see cref="IProcessLock"/> made with<br/>
/// the same name and directory is the same lock. Taking it returns an <see cref="IProcessLockHold"/>, and disposing the hold releases it.</summary>
///<remarks>A hold belongs to no thread, unlike a lock on an <see cref="IMutex"/>, which only the thread that took it can release. Any thread may<br/>
/// dispose it, so async code can hold it across awaits, and an xUnit fixture can take it as it is created and release it as it is disposed.<br/>
/// When the holder's process dies, the operating system releases the lock, so a process that crashed or was killed never leaves it held.<br/>
/// Waiting to take it is limited by a <see cref="WaitTimeout"/>, not a <see cref="LockTimeout"/>: a holder may rightly hold it for minutes, so a<br/>
/// long wait is no sign of a deadlock. Whoever gives up waiting is told what the lock is held for, by which process, and since when.</remarks>
///<remarks>The lock is a file in the directory, <c>{name}.lock</c>, which a holder keeps open in a way that keeps every other holder out. A wait<br/>
/// tries to open it every 50 milliseconds, so the lock isn't fair: whoever tries first after a release takes it. Nor is it reentrant: a second<br/>
/// take waits for the first hold like any other, even in the same process, on the same thread.<br/>
/// On Windows a holder shares the file for reading, so that a virus scanner or an indexer reading it keeps no holder out, and writes in it what it<br/>
/// holds the lock for. On Linux, macOS and the other Unix systems .NET locks the file with <c>flock</c>, which keeps out only those who also lock<br/>
/// it, and locks every file it opens, so a waiter couldn't read the file: the holder writes what it holds the lock for in a file of its own beside<br/>
/// it, named after the lock with the extension <c>.holder</c>. And since .NET's file locking can be turned off, and some file systems don't<br/>
/// support <c>flock</c>, taking the lock there checks that the file keeps a second holder out, and throws if it doesn't.<br/>
/// Nothing may delete the lock file while it can be held: on Unix a holder of a deleted file keeps out no one who creates it anew. So the directory<br/>
/// must be one that nothing cleans of files in use, and <see cref="IProcessLock"/> never deletes the file.</remarks>
public partial interface IProcessLock
{
   //review-naming: IProcessLock, after Tessaging's "endpoint process lock". Not a "gate": IThreadGate means a test tool there.
   //review-naming: IProcessLock.New, the factory, as IMonitor.New is named.
   //review-naming: purpose, the parameter of the four takes that says what the lock is to be held for.
   //review-naming: TryTake, Take, TryTakeAsync and TakeAsync, the four takes, as the brief named them.
   //review-naming: LastHoldersDescription, for what the holder that took the lock last wrote of itself.

   ///<summary>The lock named <paramref name="name"/> in <paramref name="directory"/>, which must exist: every process that takes the lock of that<br/>
   /// name in that directory takes the same lock. Who can take it is who can write files there: in a directory of a user's own, it is a lock<br/>
   /// for that user's processes.</summary>
   public static IProcessLock New(string name, DirectoryInfo directory) => new ProcessLock(name, directory);

   ///<summary>Takes the lock for <paramref name="purpose"/> as soon as no other holder holds it, and returns the hold, whose disposal releases it:<br/>
   /// or returns null if another holder still holds it when <paramref name="waitTimeout"/> has passed. <see cref="WaitTimeout.Zero"/> gives an<br/>
   /// immediate answer. <paramref name="purpose"/> says what the lock is to be held for, to those who wait for it meanwhile.</summary>
   IProcessLockHold? TryTake(string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default);

   ///<summary>As <see cref="TryTake"/>, but throws a <see cref="TimeoutException"/> saying what the lock is held for, by which process, and since<br/>
   /// when, if another holder still holds it when <paramref name="waitTimeout"/> has passed.</summary>
   IProcessLockHold Take(string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default);

   ///<summary>As <see cref="TryTake"/>, holding no thread while it waits.</summary>
   Task<IProcessLockHold?> TryTakeAsync(string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default);

   ///<summary>As <see cref="Take"/>, holding no thread while it waits.</summary>
   Task<IProcessLockHold> TakeAsync(string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default);

   ///<summary>What the holder that took the lock last wrote of itself as it took it: what it holds the lock for, which process it is, on which<br/>
   /// machine, and since when, such as <c>for running the hub, by process 4242 (Hub) on WORKSTATION, since 2026-11-26 10:15:00 +01:00</c>, the<br/>
   /// words a <see cref="Take"/> that gives up says it in. Null if no holder has written it yet, as when one took the lock this instant.<br/>
   /// For whoever is refused the lock, to say who holds it in words of its own.</summary>
   ///<remarks>A holder's release leaves what it wrote, so beside a free lock this describes the holder that released it last: read it when<br/>
   /// a take is refused, which means the lock is held.</remarks>
   string? LastHoldersDescription();
}
