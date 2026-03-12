namespace Compze.Threading;

///<summary>A read lease for an <see cref="ILock"/>. Calling dispose releases the lock.</summary>
interface IReadLock : IDisposable
{
}

///<summary>A read lease for an <see cref="ILock"/>. Calling dispose releases the lock and notifies all threads waiting on conditions to become true to reacquire the lock and reevaluate the conditions.</summary>
interface IUpdateLock : IDisposable
{
}
