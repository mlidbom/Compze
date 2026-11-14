namespace Compze.Threading;

///<summary>A read lock on an <see cref="IAwaitableCriticalSection"/>, held by code that only reads the state the critical section protects.<br/>
/// Disposing it releases the lock and wakes no wait for a condition, which is why the code holding it must not change that state:<br/>
/// <see cref="IAwaitableCriticalSection"/> tells why reads and updates are declared apart.</summary>
public interface IReadLock : IDisposable
{
}