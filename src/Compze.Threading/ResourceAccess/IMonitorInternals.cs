namespace Compze.Threading.ResourceAccess;

public interface IMonitorInternals
{
   public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace);
   internal long ContentionCount { get; }
}
