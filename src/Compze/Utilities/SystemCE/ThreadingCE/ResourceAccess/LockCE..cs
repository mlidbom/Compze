using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

//An attribute is used rather than a pragma because the roslyn analyzers are confused by the multiple files of this partial class and keep calling
//the pragma redundant and still showing the warning about disposable fields. After moving the pragma to the file the analyzer wanted it in this
//time the warning goes away, only to come back with the next restart of visual studio
[assembly: SuppressMessage("Design",
                           "CA1001:Types that own disposable fields should be disposable",
                           Justification = "By creating the locks only once in the constructor usages become zero-allocation operations. By always referencing them by the concrete type inlining remains possible.",
                           Scope = "type",
                           Target = "~T:Compze.SystemCE.ThreadingCE.ResourceAccess.MonitorCE")]

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

///<summary>The monitor class exposes a rather obscure, brittle and easily misused API in my opinion. This class attempts to adapt it to something that is reasonably understandable and less brittle.</summary>
public partial class LockCE
{
   readonly object _timeoutLock = new();
   IReadOnlyList<EnterLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<EnterLockTimeoutException>();
   internal static Action? OnTimeOut;


   public void SetTimeToWaitForStackTrace(TimeSpan timeToWaitForStackTrace) => _stackTraceFetchTimeout = timeToWaitForStackTrace;


   void ReleaseLock()
   {
      UpdateAnyRegisteredTimeoutExceptions();
      _monitor.ReleaseLock();
   }

   Exception RegisterTimeoutException()
   {
      OnTimeOut?.Invoke();
      lock(_timeoutLock)
      {
         var exception = new EnterLockTimeoutException(Timeout, _stackTraceFetchTimeout);
         OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _timeOutExceptionsOnOtherThreads, exception);
         return exception;
      }
   }

   void UpdateAnyRegisteredTimeoutExceptions()
   {
      if(_timeOutExceptionsOnOtherThreads.Count > 0)
      {
         lock(_timeoutLock)
         {
            var stackTrace = new StackTrace(fNeedFileInfo: true);
            foreach(var exception in _timeOutExceptionsOnOtherThreads)
            {
               exception.SetBlockingThreadsDisposeStackTrace(stackTrace);
            }

            Interlocked.Exchange(ref _timeOutExceptionsOnOtherThreads, new List<EnterLockTimeoutException>());
         }
      }
   }
}
