namespace Compze.Threading;

public interface ILockInfo
{
   LockTimeout LockTimeout { get; }
   long ContentionCount { get; }
}
