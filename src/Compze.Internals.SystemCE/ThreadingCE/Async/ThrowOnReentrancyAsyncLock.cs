using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.Async._private;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.SystemCE;
using Compze.Threading;

namespace Compze.Internals.SystemCE.ThreadingCE.Async;

///<summary>A lock that may be held across awaits, which synchronous and asynchronous code take alike, and which throws at once when<br/>
/// it is taken from inside its own hold: on the async flow that holds it, or in work started inside the hold while the hold lasts.<br/>
/// Independent flows take turns. Taking it times out after its <see cref="LockTimeout"/>.</summary>
///<remarks>It's for what only one may use at a time, with no exception, such as a database connection, which no provider lets two<br/>
/// operations use at once. <see cref="IAsyncLockCE"/> lets a nested use in, and so it lets in work started inside the hold too,<br/>
/// such as children the holder starts and awaits together, which then use the resource at once. A plain lock that isn't<br/>
/// re-entrant, such as a <see cref="SemaphoreSlim"/>, makes a nested use wait for itself until its timeout, and children the holder<br/>
/// awaits wait for it the same way.</remarks>
///<remarks>Work started inside the hold that takes the lock only once the hold has ended is an independent flow, and takes its turn.<br/>
/// <see cref="IsHeldOnThisFlow"/> tells code that may run inside the hold, such as a callback, whether taking the lock would throw.<br/>
/// It isn't disposable, as <see cref="IAsyncLockCE"/> isn't: nothing it holds needs disposing, and disposing a lock that is held,<br/>
/// or waited for, would break its holder's release of it and its waiters.</remarks>
public interface IThrowOnReentrancyAsyncLock
{
   static IThrowOnReentrancyAsyncLock WithDefaultTimeout() => new ThrowOnReentrancyAsyncLock(LockTimeout.Default);
   static IThrowOnReentrancyAsyncLock New(LockTimeout timeout) => new ThrowOnReentrancyAsyncLock(timeout);

   Task<Unit> LockedAsync(Func<Task> lockedAction);
   Task<TReturn> LockedAsync<TReturn>(Func<Task<TReturn>> lockedAction);
   Unit Locked(Action lockedAction);
   TReturn Locked<TReturn>(Func<TReturn> lockedAction);

   ///<summary>Whether this async flow holds the lock: it took it, or was started inside its hold, and the hold lasts. Taking the lock<br/>
   /// then throws.</summary>
   bool IsHeldOnThisFlow { get; }

#pragma warning disable CA1001 // The semaphore holds nothing to dispose: SemaphoreSlim allocates only its AvailableWaitHandle, which this never asks for. See IThrowOnReentrancyAsyncLock's remarks.
   private sealed class ThrowOnReentrancyAsyncLock : IThrowOnReentrancyAsyncLock
#pragma warning restore CA1001
   {
      readonly SemaphoreSlim _semaphore = new(1, 1);
      ///<summary>The hold this flow took, or inherited from the flow it was started on, which may have ended since.</summary>
      readonly AsyncLocal<Hold?> _holdOfThisFlow = new();
      readonly AsyncLockTimeouts _timeouts;

      internal ThrowOnReentrancyAsyncLock(LockTimeout timeout) => _timeouts = new AsyncLockTimeouts(timeout);

      public bool IsHeldOnThisFlow => _holdOfThisFlow.Value is { HasEnded: false };

      public async Task<Unit> LockedAsync(Func<Task> lockedAction) => await LockedAsync(lockedAction.ToAsyncFunc()).caf();

      public async Task<TReturn> LockedAsync<TReturn>(Func<Task<TReturn>> lockedAction)
      {
         RefuseEntryFromInsideTheHold();
         if(!await _semaphore.WaitAsync(_timeouts.Timeout).caf())
         {
            throw _timeouts.TimedOut();
         }

         var hold = new Hold();
         _holdOfThisFlow.Value = hold; //An async method's changes to its flow don't reach its caller's.
         try
         {
            return await lockedAction().caf();
         }
         finally
         {
            Release(hold);
         }
      }

      public Unit Locked(Action lockedAction) => Locked(lockedAction.ToFunc());

      public TReturn Locked<TReturn>(Func<TReturn> lockedAction)
      {
         RefuseEntryFromInsideTheHold();
         if(!_semaphore.Wait(_timeouts.Timeout))
         {
            throw _timeouts.TimedOut();
         }

         var holdBefore = _holdOfThisFlow.Value;
         var hold = new Hold();
         _holdOfThisFlow.Value = hold;
         try
         {
            return lockedAction();
         }
         finally
         {
            Release(hold);
            _holdOfThisFlow.Value = holdBefore; //A synchronous method changes its caller's flow: this leaves it as it was.
         }
      }

      void RefuseEntryFromInsideTheHold() =>
         State.Assert(!IsHeldOnThisFlow,
                      () => $"An {nameof(IThrowOnReentrancyAsyncLock)} was taken from inside its own hold: by a nested use on the async flow that holds it, or by work started inside the hold while the hold lasts. It refuses such entries, since they would use what it guards together with its holder.");

      void Release(Hold hold)
      {
         hold.End();
         _timeouts.TheHolderReleases();
         _semaphore.Release();
      }

      ///<summary>One holding of the lock, from the flow that took it until that flow leaves it. Work started inside it carries it, and once<br/>
      /// it has ended that work is an independent flow.</summary>
      sealed class Hold
      {
         volatile bool _hasEnded;
         internal bool HasEnded => _hasEnded;
         internal void End() => _hasEnded = true;
      }
   }
}
