using Compze.Threading.Exceptions;

namespace Compze.Threading.Interprocess._private.Exceptions;

///<summary>Thrown when a cross-process mutex lock acquisition times out after <see cref="LockTimeout"/>.</summary>
class TakeMutexLockTimeoutException(LockTimeout timeout, WaitTimeout stackTraceFetchTimeout)
   : TakeLockTimeoutException($"Timed out awaiting interprocess mutex lock after {timeout}. This likely indicates a cross-process deadlock.", stackTraceFetchTimeout);
