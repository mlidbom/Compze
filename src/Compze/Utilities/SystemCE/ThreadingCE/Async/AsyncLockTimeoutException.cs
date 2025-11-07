using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.Async;

public class AsyncLockTimeoutException : Exception
{
   internal AsyncLockTimeoutException(TimeSpan timeout) : base($"Timed out awaiting async lock after {timeout}. This likely indicates an in-memory deadlock.")
   {
   }
}
