using Compze.Contracts;
using Compze.Threading;

namespace Compze.Internals.SystemCE._private;

///<summary>
/// Lazy initialization of a field by double-checked locking that locks on an object the caller already has,<br/>
/// so that no lock object is allocated. Reading the field once it is set takes no lock.
///</summary>
///<remarks>
/// Exists so that a <see cref="LazyCE{TValue}"/> is one small object. With an <see cref="IMonitor"/> of its own,<br/>
/// each instance cost ten allocations and 424 bytes, and creating one and reading it once took four times as long.
///</remarks>
static class DoubleCheckedLockingOnAnExistingObject
{
   ///<summary>
   /// Returns <paramref name="field"/> if it is set. Otherwise locks <paramref name="lockingOn"/> and, unless another thread<br/>
   /// set <paramref name="field"/> while this one waited for the lock, sets it to what <paramref name="createValue"/> returns.
   ///</summary>
   ///<remarks>
   /// <paramref name="createValue"/> runs while the lock is held, so it runs once, however many threads call this at once.<br/>
   /// If it throws, <paramref name="field"/> stays unset and the next call runs it again.<br/>
   /// The field is read with acquire and written with release semantics (<see cref="Volatile"/>), so a thread that sees the<br/>
   /// value also sees everything <paramref name="createValue"/> wrote before returning it.
   ///</remarks>
   ///<remarks>
   /// The lock on <paramref name="lockingOn"/> is held only while <paramref name="createValue"/> runs, so this thread already holding it<br/>
   /// means <paramref name="createValue"/> is reading the value it is creating, directly or through another value created this way,<br/>
   /// or that code which locked on <paramref name="lockingOn"/> from outside is reading it. Entering the lock again would run<br/>
   /// <paramref name="createValue"/> again, endlessly, until the stack overflowed and ended the process; it throws instead.
   ///</remarks>
   ///<exception cref="TimeoutException">Another thread held the lock longer than <see cref="LockTimeout.Default"/>, most likely because of a deadlock.</exception>
   ///<exception cref="InvalidOperationException">This thread already holds the lock on <paramref name="lockingOn"/>.</exception>
   internal static TValue GetOrCreate<TValue>(ref TValue? field, Func<TValue> createValue, object lockingOn) where TValue : class =>
      Volatile.Read(ref field) ?? CreateUnlessAnotherThreadAlreadyHas(ref field, createValue, lockingOn);

   static TValue CreateUnlessAnotherThreadAlreadyHas<TValue>(ref TValue? field, Func<TValue> createValue, object lockingOn) where TValue : class
   {
      State.Assert(!Monitor.IsEntered(lockingOn),
                   () => "This thread already holds this LazyCE's lock, which is held only while its value is being created. "
                       + "Either its factory read the value it is creating, directly or through another LazyCE, "
                       + "or code that locked on the LazyCE from outside read its value, which LazyCE's documentation forbids.");

      //rationale: A raw Monitor instead of the Compze.Threading locking primitives, which allocate an IMonitor per instance:
      //   see dev_docs/code_standard_exceptions/raw-monitor-in-lazy-initialization.md.
      if(!Monitor.TryEnter(lockingOn, LockTimeout.Default))
         throw new TimeoutException($"Timed out after {LockTimeout.Default.Value} waiting for another thread to finish creating a lazily created value. This most likely indicates a deadlock.");

      try
      {
         var setByAnotherThread = field;
         if(setByAnotherThread != null) return setByAnotherThread;

         var created = createValue();
         Volatile.Write(ref field, created);
         return created;
      }
      finally
      {
         Monitor.Exit(lockingOn);
      }
   }
}
