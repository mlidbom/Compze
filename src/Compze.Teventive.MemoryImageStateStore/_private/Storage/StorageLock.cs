using Compze.Contracts;
using Compze.Threading;
using Compze.Threading.ResourceAccess;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.MemoryImageStateStore._private.Storage;

///<summary>The lock of a memory image state store's storage, such as its folder or its tables, which one memory image at a time holds: taken by<br/>
/// the first load, and released when the storage is disposed, but never while a write of this memory image can still land in the storage.</summary>
///<remarks>Every use of the storage is made through the lock: a load, a write outside any unit of work, such as of every taggregate anew, and a<br/>
/// unit of work's write, which is under way from its preparing until its unit of work has completed, for as long as it can still land.<br/>
/// Disposing refuses each use that begins after it, waits for those under way to end, and only then releases the lock. So the next memory<br/>
/// image to take the lock never shares the storage with a write of this one, which would land over what that one writes.</remarks>
///<remarks>A unit of work that hasn't prepared its write as disposing begins fails as it prepares, and rolls back. One that has prepared it<br/>
/// commits or rolls back as it would have, and the lock is released after that. Disposing waits at most<br/>
/// <see cref="WaitForTheUsesUnderWay"/>: should a use still be under way then, it throws, and the lock stays held, until the process ends,<br/>
/// rather than be released while a write can still land. A first load waiting for another memory image to release the storage is a use<br/>
/// under way too.</remarks>
sealed class StorageLock : IDisposable
{
   ///<summary>How long disposing waits for the uses under way to end. Each ends by itself: the wait is bounded so that a use that never ends<br/>
   /// fails disposing loudly, instead of hanging it.</summary>
   static readonly WaitTimeout WaitForTheUsesUnderWay = WaitTimeout.Minutes(1);

   readonly string _whereItIsKept;
   readonly Func<IDisposable> _take;
   readonly IAwaitableThreadShared<Holding> _holding = IAwaitableThreadShared.New(new Holding());

   ///<summary>The lock of the storage of the store kept in <paramref name="whereItIsKept"/>, which <paramref name="take"/> takes, or throws<br/>
   /// the refusal of the memory image that waited for another to release it.</summary>
   internal StorageLock(string whereItIsKept, Func<IDisposable> take)
   {
      _whereItIsKept = whereItIsKept;
      _take = take;
   }

   ///<summary>Runs <paramref name="load"/>, a load of the storage, holding the lock: the first load takes it first. A first load that fails<br/>
   /// takes nothing, and the next tries again. Called under the store's loading monitor, so no two loads take the lock.</summary>
   internal TResult WhileLoading<TResult>(Func<TResult> load)
   {
      var isTaken = _holding.Update(it => it.BeginALoad(_whereItIsKept));
      try
      {
         if(!isTaken)
         {
            var taken = _take();
            _holding.Update(it => it.Hold(taken));
         }

         return load();
      }
      finally
      {
         EndAUse();
      }
   }

   ///<summary>Runs <paramref name="write"/>, a write of the storage outside any unit of work, such as of every taggregate anew.</summary>
   internal void WhileWriting(Action write)
   {
      _holding.Update(it => it.BeginAWrite(_whereItIsKept));
      try
      {
         write();
      }
      finally
      {
         EndAUse();
      }
   }

   ///<summary>Prepares a unit of work's write with <paramref name="prepare"/>, which is given the write under way, for the prepared write to<br/>
   /// end once its unit of work has completed. If <paramref name="prepare"/> throws, the write ends at once, as nothing can land.</summary>
   internal TPreparedWrite PreparingAWrite<TPreparedWrite>(Func<WriteUnderWay, TPreparedWrite> prepare)
   {
      _holding.Update(it => it.BeginAWrite(_whereItIsKept));
      var underWay = new WriteUnderWay(this);
      try
      {
         return prepare(underWay);
      }
      catch
      {
         underWay.End();
         throw;
      }
   }

   void EndAUse() => _holding.Update(it => it.EndAUse());

   ///<summary>Refuses every use from now on, waits for the uses under way to end, then releases the lock, if a load has taken it, for the<br/>
   /// next memory image to take.</summary>
   public void Dispose()
   {
      _holding.Update(it => it.RefuseEveryUse());
      if(!_holding.TryUpdateWhen(it => it.HasNoUseUnderWay, it => it.TakeTheLockToRelease(), out var toRelease, timeout: WaitForTheUsesUnderWay))
         throw new TimeoutException($"The memory image state store kept in {_whereItIsKept} is disposed, and waited {WaitForTheUsesUnderWay} for its uses of the storage under way to end: {_holding.Read(it => it.UsesUnderWay)} still haven't. "
                                  + "It keeps holding the storage until the process ends, so that no other memory image takes it while a write of this one can still land.");
      toRelease?.Dispose();
   }

   ///<summary>A unit of work's write, under way from its preparing until its unit of work has completed: <see cref="End"/>.</summary>
   internal sealed class WriteUnderWay
   {
      readonly StorageLock _lock;
      bool _hasEnded;

      internal WriteUnderWay(StorageLock theLock) => _lock = theLock;

      ///<summary>The unit of work has completed, or the write failed to prepare: the write can no longer land.</summary>
      internal void End()
      {
         State.Assert(!_hasEnded, () => "A write of a memory image state store's storage ended twice.");
         _hasEnded = true;
         _lock.EndAUse();
      }
   }

   ///<summary>Whether the lock is taken, how many uses of the storage are under way, and whether disposing has begun.</summary>
   sealed class Holding
   {
      IDisposable? _taken;
      bool _refusesEveryUse;

      internal int UsesUnderWay { get; private set; }

      internal bool HasNoUseUnderWay => UsesUnderWay == 0;

      ///<summary>Begins a load; whether a load has taken the lock already.</summary>
      internal bool BeginALoad(string whereItIsKept)
      {
         AssertNotDisposed(whereItIsKept);
         UsesUnderWay++;
         return _taken != null;
      }

      internal void BeginAWrite(string whereItIsKept)
      {
         AssertNotDisposed(whereItIsKept);
         State.Assert(_taken != null, () => $"The memory image state store kept in {whereItIsKept} writes before it has loaded, and so before it holds its storage.");
         UsesUnderWay++;
      }

      void AssertNotDisposed(string whereItIsKept) =>
         State.Assert(!_refusesEveryUse, () => $"The memory image state store kept in {whereItIsKept} is disposed, as its container is, so it neither loads nor writes any more: another memory image may hold its storage by now.");

      internal void Hold(IDisposable taken)
      {
         State.Assert(_taken == null, () => "Two loads of a memory image state store took its storage's lock: loads are made one at a time, under the store's loading monitor.");
         _taken = taken;
      }

      internal void EndAUse() => UsesUnderWay--;

      internal void RefuseEveryUse() => _refusesEveryUse = true;

      ///<summary>The lock taken, to release now, once: a second disposal releases nothing.</summary>
      internal IDisposable? TakeTheLockToRelease()
      {
         var taken = _taken;
         _taken = null;
         return taken;
      }
   }
}
