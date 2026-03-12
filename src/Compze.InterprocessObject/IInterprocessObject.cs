using Compze.SystemCE;

namespace Compze.InterprocessObject;

///<summary>A strongly-typed object shared across processes. Changes made by one process are immediately visible to all others.
///<para>The object is persisted to disk and survives process restarts and reboots. All reads and updates are atomic and protected by a cross-process mutex.</para>
///</summary>
public interface IInterprocessObject<T> where T : class
{
   //core
   ///<summary>Acquires a read lock, passes the shared object to <paramref name="read"/>, returns the result, then releases the lock.</summary>
   TResult Read<TResult>(Func<T, TResult> read);

   ///<summary>Blocks until <paramref name="condition"/> returns true for the shared object, then acquires a read lock, executes <paramref name="read"/>, and returns its result.</summary>
   TResult ReadWhen<TResult>(Func<T, bool> condition, Func<T, TResult> read, TimeSpan? timeout = null);

   ///<summary>Acquires an update lock, passes the shared object to <paramref name="update"/>, returns the result, persists the change, then releases the lock.</summary>
   TResult Update<TResult>(Func<T, TResult> update);

   ///<summary>Blocks until <paramref name="condition"/> returns true for the shared object, then acquires an update lock, executes <paramref name="update"/>, persists the change, and returns its result.</summary>
   TResult UpdateWhen<TResult>(Func<T, bool> condition, Func<T, TResult> update, TimeSpan? timeout = null);

   ///<summary>Blocks until <paramref name="condition"/> returns true or <paramref name="timeout"/> expires. If the condition was met, acquires an update lock, executes <paramref name="update"/>, and persists the change. Returns true if the update was performed, false if the wait timed out.</summary>
   bool TryUpdateWhen(Func<T, bool> condition, Action<T> update, TimeSpan? timeout = null);

   //Default implementations
   ///<summary>Acquires a read lock, passes the shared object to <paramref name="read"/>, then releases the lock.</summary>
   Unit Read(Action<T> read) => Read(read.ToFunc());

   ///<summary>Acquires an update lock, passes the shared object to <paramref name="update"/>, persists the change, then releases the lock.</summary>
   Unit Update(Action<T> update) => Update(update.ToFunc());

   ///<summary>Deletes the backing file from disk, destroying the shared state.</summary>
   void Delete();
}
