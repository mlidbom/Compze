namespace Compze.Threading;

///<summary>A held lock on an <see cref="ICriticalSection"/>. Calling dispose releases the lock.</summary>
public interface ILock : IDisposable
{
}
