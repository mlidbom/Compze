namespace Compze.Threading;

///<summary>Internal diagnostics interface for locks. Allows configuring how long to wait when capturing the blocking thread's stack trace on timeout.</summary>
public interface ILockInternals
{
   ///<summary>Sets how long a <see cref="Exceptions.TakeLockTimeoutException"/> will wait to capture the blocking thread's stack trace before giving up.</summary>
   public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace);
}
