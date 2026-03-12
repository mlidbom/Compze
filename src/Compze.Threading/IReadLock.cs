namespace Compze.Threading;

///<summary>A read lock on an <see cref="IAwaitableCriticalSection"/>. Calling dispose releases the lock.</summary>
public interface IReadLock : IDisposable
{
}