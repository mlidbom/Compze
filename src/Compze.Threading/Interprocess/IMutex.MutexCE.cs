using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Compze.Threading.ResourceAccess;
using Compze.Threading.ResourceAccess.Exceptions;
using Compze.Threading.Utilities;

namespace Compze.Threading.Interprocess;

public partial interface IMutex
{
#pragma warning disable CS0618 // Type or member is obsolete
   private class MutexCE : IMutex, ILockInternals
#pragma warning restore CS0618 // Type or member is obsolete
   {
      readonly Mutex _mutex;
      readonly LockTimeout _lockTimeout;
      readonly Action? _onAbandonedMutex;
#pragma warning disable CA2213
      readonly IDisposable _lockDisposer;
#pragma warning restore CA2213
      readonly ILock _timeoutLock = New();
      long _contentionCount;

      static readonly WaitTimeout DefaultTimeToWaitForStackTrace = WaitTimeout.Seconds(1);
      WaitTimeout _stackTraceFetchTimeout = DefaultTimeToWaitForStackTrace;

      public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace) => _stackTraceFetchTimeout = timeToWaitForStackTrace;

      public MutexCE(string mutexName, LockTimeout? lockTimeout, Action? onAbandonedMutex)
      {
         _lockTimeout = lockTimeout ?? LockTimeout.Default;
         _onAbandonedMutex = onAbandonedMutex;
         _mutex = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                     ? WindowsGlobalMutex(mutexName)
                     : NonWindowsGlobalMutex(mutexName);
         _lockDisposer = new LockDisposer(ReleaseLock);
      }

      ///<summary>On windows a global mutex must be configured with certain access rules or access will fail with exceptions.</summary>
      [SupportedOSPlatform("windows")]
      static Mutex WindowsGlobalMutex(string mutexName)
      {
         var security = new MutexSecurity();
         security.AddAccessRule(new MutexAccessRule(
                                   new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null),
                                   MutexRights.FullControl,
                                   AccessControlType.Allow));
         return MutexAcl.Create(initiallyOwned: false, mutexName, out _, security);
      }

      static Mutex NonWindowsGlobalMutex(string mutexName) => new(initiallyOwned: false, name: mutexName);

      public long ContentionCount => Interlocked.Read(ref _contentionCount);

      public IDisposable TakeLock(LockTimeout? timeout = null)
      {
         var effectiveTimeout = timeout ?? _lockTimeout;

         bool acquired;
         try
         {
            acquired = _mutex.WaitOne(TimeSpan.Zero);
            if(!acquired)
            {
               Interlocked.Increment(ref _contentionCount);
               try
               {
                  acquired = _mutex.WaitOne(effectiveTimeout);
               }
               catch(AbandonedMutexException)
               {
                  _onAbandonedMutex?.Invoke();
                  acquired = true; // The mutex IS acquired when this exception is thrown. https://learn.microsoft.com/en-us/dotnet/api/System.Threading.AbandonedMutexException
               }
            }
         }
         catch(AbandonedMutexException)
         {
            _onAbandonedMutex?.Invoke();
            acquired = true;
         }

         if(!acquired) throw RegisterTimeoutException();

         return _lockDisposer;
      }

      void ReleaseLock()
      {
         UpdateAnyRegisteredTimeoutExceptions();
         _mutex.ReleaseMutex();
      }

      IReadOnlyList<TakeMutexLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<TakeMutexLockTimeoutException>();

      Exception RegisterTimeoutException() => _timeoutLock.Locked(() =>
      {
         var exception = new TakeMutexLockTimeoutException(_lockTimeout, _stackTraceFetchTimeout);
         OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _timeOutExceptionsOnOtherThreads, exception);
         return exception;
      });

      void UpdateAnyRegisteredTimeoutExceptions()
      {
         // ReSharper disable once InconsistentlySynchronizedField
         if(_timeOutExceptionsOnOtherThreads.Count > 0)
         {
            _timeoutLock.Locked(() =>
            {
               var stackTrace = new StackTrace(fNeedFileInfo: true);
               foreach(var exception in _timeOutExceptionsOnOtherThreads)
               {
                  exception.SetBlockingThreadsDisposeStackTrace(stackTrace);
               }

               Interlocked.Exchange(ref _timeOutExceptionsOnOtherThreads, new List<TakeMutexLockTimeoutException>());
            });
         }
      }

      public void Dispose() => _mutex.Dispose();
   }
}
