namespace Compze.Threading.ResourceAccess;

public interface ILockInternals
{
   public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace);
}
