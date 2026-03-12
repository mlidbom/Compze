namespace Compze.Threading;

///<summary>An update lock for an <see cref="IAwaitableCriticalSection"/>. Calling dispose releases the lock and notifies all threads waiting on conditions to become true to reacquire the lock and reevaluate the conditions.</summary>
public interface IUpdateLock : IDisposable
{
}
