namespace Compze.Threading;

public interface ILockInternals
{
   public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace);
}
