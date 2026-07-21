namespace Compze.Threading.Exceptions.Private;

///<summary>Thrown when an in-process monitor lock acquisition times out after <see cref="LockTimeout"/>.</summary>
class TakeMonitorLockTimeoutException(LockTimeout timeout, WaitTimeout stackTraceFetchTimeout)
   : TakeLockTimeoutException($"Timed out awaiting monitor lock after {timeout}. This likely indicates an in-memory deadlock.", stackTraceFetchTimeout);
