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
public partial class MonitorCE
{
   readonly object _timeoutLock = new();
   IReadOnlyList<EnterLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<EnterLockTimeoutException>();
   internal static Action? OnTimeOut;

   IDisposable TakeLock(LockType lockType) => TakeLock(Timeout, lockType);

   IDisposable TakeLock(TimeSpan timeout, LockType lockType) => TryTakeLock(timeout, lockType) ?? throw RegisterTimeoutException();

   public void SetTimeToWaitForStackTrace(TimeSpan timeToWaitForStackTrace) => _stackTraceFetchTimeout = timeToWaitForStackTrace;

   void Exit()
   {
      UpdateAnyRegisteredTimeoutExceptions();
      Monitor.Exit(_lockObject);
   }

   IDisposable? TryTakeLock(TimeSpan timeout, LockType lockType)
   {
      if(Monitor.TryEnter(_lockObject)) return LockFor(lockType); //This will never block and calling it first improves performance quite a bit.

      var lockTaken = false;
      try
      {
         Monitor.TryEnter(_lockObject, timeout, ref lockTaken);
         return lockTaken ? LockFor(lockType) : null;
      }
      catch(Exception) //It is rare, but apparently possible, for TryEnter to throw an exception after the lock is taken. So we need to catch it and call Monitor.Exit if that happens to avoid leaking locks.
      {
         if(lockTaken) Monitor.Exit(_lockObject);;
         throw;
      }
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
