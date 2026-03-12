namespace Compze.Threading;

///<summary>Exposes diagnostic information about a lock.</summary>
public interface ILockInfo
{
   ///<summary>The timeout used when acquiring the lock if no explicit timeout is provided.</summary>
   LockTimeout LockTimeout { get; }
   ///<summary>The number of lock acquisition attempts that had to wait because the lock was already held.</summary>
   long ContentionCount { get; }
}
