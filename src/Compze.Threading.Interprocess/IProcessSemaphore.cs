namespace Compze.Threading.Interprocess;

///<summary>A semaphore between processes: of its <see cref="Units"/> units, such as sandboxes that may run at once, each is held by one holder at a<br/>
/// time, whichever process on the machine it is in. Every <see cref="IProcessSemaphore"/> made with the same name and directory is the same<br/>
/// semaphore. Taking units returns an <see cref="IProcessSemaphoreHold"/>, and disposing the hold releases them.</summary>
///<remarks>A take of several units takes them all, or none: a holder never holds a part of what it needs while it waits for the rest, so two<br/>
/// holders can't each hold a part of what the other needs. Takes are served in the order they came, so a take of many units isn't starved by takes<br/>
/// of few: a take waits while an earlier one waits, even when there are units enough for it.<br/>
/// When a holder's process dies, the operating system releases its units, and a process that dies waiting gives up its place, so a process that<br/>
/// crashed or was killed never leaves units held, nor the takes after it waiting. Windows' named <see cref="System.Threading.Semaphore"/> has no<br/>
/// holders: the units of a process that dies without releasing them are gone until it is made anew.<br/>
/// A hold belongs to no thread: any thread may dispose it, so async code can hold it across awaits. Waiting for units is limited by a<br/>
/// <see cref="WaitTimeout"/>, as a holder may rightly hold them for minutes, and whoever gives up waiting is told who holds the units and who waits<br/>
/// ahead of it: what for, which process, and since when.</remarks>
///<remarks>Each unit is a lock file in the directory, <c>{name}.unit-{index}.lock</c>, held as an <see cref="IProcessLock"/>'s file is, so that the<br/>
/// operating system releases it when its holder dies. The takes waiting for units are in a queue in an<br/>
/// <see cref="ResourceAccess.IInterprocessObject{T}"/> beside them, and each waiting take holds a lock file of its own, a waiter slot,<br/>
/// <c>{name}.waiter-{index}.lock</c>, so that the others know when it dies: there are as many waiter slot files as there have been takes waiting at<br/>
/// once. Only the take first in the queue takes units: it takes each as it is released, and leaves the queue once it holds them all.<br/>
/// A release tells the takes that wait at once; the death of a holder, or of a waiting take, tells nothing, so a waiting take also looks every<br/>
/// second. Nothing may delete the files while they can be held, so the directory must be one that nothing cleans of files in use.<br/>
/// All who use a semaphore use the same number of units: one made with another number takes units only once none are held or waited for, and<br/>
/// from then on the semaphore has that number.</remarks>
public partial interface IProcessSemaphore
{
   //review-naming: IProcessSemaphore, a counting semaphore between processes, after IProcessLock, which a holder holds as it holds a unit.
   //review-naming: IProcessSemaphore.Units, and units, for what it has a limited number of, and the parameter of the takes saying how many to take.
   //review-naming: DescribeHoldersAndWaiters, for the description of who holds its units and who waits for them.

   ///<summary>The semaphore named <paramref name="name"/> in <paramref name="directory"/>, which must exist, with <paramref name="units"/> units: every<br/>
   /// process that makes the semaphore of that name in that directory makes the same semaphore. Who can take its units is who can write files<br/>
   /// there: in a directory of a user's own, it is a semaphore for that user's processes.</summary>
   public static IProcessSemaphore New(string name, int units, DirectoryInfo directory) => new ProcessSemaphore(name, units, directory);

   ///<summary>How many units it has: how many holders may hold one each at once.</summary>
   int Units { get; }

   ///<summary>Takes <paramref name="units"/> units for <paramref name="purpose"/>, all of them at once, as soon as they are free and no earlier take<br/>
   /// waits, and returns the hold, whose disposal releases them: or returns null if it hasn't taken them when <paramref name="waitTimeout"/> has<br/>
   /// passed. <see cref="WaitTimeout.Zero"/> gives an immediate answer. <paramref name="purpose"/> says what the units are to be held for, to those<br/>
   /// who wait meanwhile. <paramref name="units"/> is at least 1, and at most <see cref="Units"/>.</summary>
   IProcessSemaphoreHold? TryTake(int units, string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default);

   ///<summary>As <see cref="TryTake"/>, but throws a <see cref="TimeoutException"/> saying who holds the units, who waits ahead, what for, which<br/>
   /// processes, and since when, if it hasn't taken them when <paramref name="waitTimeout"/> has passed.</summary>
   IProcessSemaphoreHold Take(int units, string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default);

   ///<summary>As <see cref="TryTake"/>, holding no thread while it waits.</summary>
   Task<IProcessSemaphoreHold?> TryTakeAsync(int units, string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default);

   ///<summary>As <see cref="Take"/>, holding no thread while it waits.</summary>
   Task<IProcessSemaphoreHold> TakeAsync(int units, string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default);

   ///<summary>Says who holds its units now, and who waits for them, in the order they came: how many units, what for, which process, and since when.<br/>
   /// For a holder that tells of a long wait while it waits, which giving up and taking again would lose its place in the order.</summary>
   string DescribeHoldersAndWaiters();
}
