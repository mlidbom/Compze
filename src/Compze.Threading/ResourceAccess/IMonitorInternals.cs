namespace Compze.Threading.ResourceAccess;

[Obsolete("For internal use only")]
public interface IMonitorInternals
{
   public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace);
   internal long ContentionCount { get; }
}
