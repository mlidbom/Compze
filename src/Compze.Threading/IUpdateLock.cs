namespace Compze.Threading;

///<summary>An update lock on an <see cref="IAwaitableCriticalSection"/>, held by code that changes the state the critical section protects.<br/>
/// Disposing it releases the lock and wakes all waits for a condition, which take the lock again and evaluate their conditions.</summary>
public interface IUpdateLock : IDisposable
{
}
