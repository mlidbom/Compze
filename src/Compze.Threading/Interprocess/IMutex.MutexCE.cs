using Compze.Threading.Exceptions;
using Compze.Threading.Utilities;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Compze.Threading.SystemCE;

namespace Compze.Threading.Interprocess;

public partial interface IMutex
{
#pragma warning disable CS0618 // Type or member is obsolete
   private class MutexCE : IMutex, ILockInternals
#pragma warning restore CS0618 // Type or member is obsolete
   {
      readonly Mutex _mutex;
      public LockTimeout LockTimeout { get; }
      readonly Action? _onAbandonedMutex;
#pragma warning disable CA2213
      readonly LockDisposer _lockDisposer;
#pragma warning restore CA2213
      readonly IMonitor _timeoutMonitor = IMonitor.New();
      long _contentionCount;

      static readonly WaitTimeout DefaultTimeToWaitForStackTrace = WaitTimeout.Seconds(1);
      WaitTimeout _stackTraceFetchTimeout = DefaultTimeToWaitForStackTrace;

      public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace) => _stackTraceFetchTimeout = timeToWaitForStackTrace;

      public bool IsGlobal { get; }
      public string Name { get; }

      public MutexCE(string name, bool global, LockTimeout? lockTimeout, Action? onAbandonedMutex)
      {
         if(name.Contains('\\', StringComparison.Ordinal)) throw new ArgumentException("Name must not contain backslashes", nameof(name));

         IsGlobal = global;

         Name = global ? $@"Global\{name}" : $@"Local\{name}";

         LockTimeout = lockTimeout ?? LockTimeout.Default;
         _onAbandonedMutex = onAbandonedMutex;
         if(IsGlobal && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
         {
            _mutex = WindowsGlobalMutex(name: Name);
         } else
         {
            _mutex = new(initiallyOwned: false, name: Name);
         }

         _lockDisposer = new LockDisposer(ReleaseLock);
      }

      public long ContentionCount => Interlocked.Read(ref _contentionCount);

      public ILock TakeLock(LockTimeout? timeout = null) => TryTakeLock(timeout) ?? throw RegisterTimeoutException();

      static readonly TimeSpan InterruptPollingInterval = TimeSpan.FromSeconds(1);

      public ILock? TryTakeLock(LockTimeout? timeout = null)
      {
         var effectiveTimeout = timeout ?? LockTimeout;

         bool acquired;
         try
         {
            acquired = _mutex.WaitOne(TimeSpan.Zero);
            if(!acquired && timeout != TimeSpan.Zero)
            {
               Interlocked.Increment(ref _contentionCount);

               // Poll so the thread periodically returns to managed code, allowing a pending Thread.Interrupt to fire as ThreadInterruptedException.
               // Without this, Mutex.WaitOne on Linux enters an unmanaged wait that is not interruptible, causing Thread.Interrupt to have no effect until the mutex is released.
               var deadline = DateTime.UtcNow + effectiveTimeout.ToTimeSpan();
               while(true)
               {
                  var remaining = deadline - DateTime.UtcNow;
                  if(remaining <= TimeSpan.Zero)
                  {
                     acquired = false;
                     break;
                  }

                  acquired = _mutex.WaitOne(TimeSpan.Min(remaining, InterruptPollingInterval));
                  if(acquired) break;
               }
            }
         }
         catch(AbandonedMutexException)
         {
            _onAbandonedMutex?.Invoke();
            acquired = true;
         }

         return acquired ? _lockDisposer : null;
      }

      ///<summary>Creates a mutex configured with certain access rules required on windows to prevent exceptions when used across login sessions.</summary>
      [SupportedOSPlatform("windows")]
      static Mutex WindowsGlobalMutex(string name)
      {
         var security = new MutexSecurity();
         security.AddAccessRule(new MutexAccessRule(
                                   new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null),
                                   MutexRights.FullControl,
                                   AccessControlType.Allow));
         return MutexAcl.Create(initiallyOwned: false, name, out _, security);
      }

      void ReleaseLock()
      {
         UpdateAnyRegisteredTimeoutExceptions();
         _mutex.ReleaseMutex();
      }

      IReadOnlyList<TakeMutexLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<TakeMutexLockTimeoutException>();

      Exception RegisterTimeoutException() => _timeoutMonitor.Locked(() =>
      {
         var exception = new TakeMutexLockTimeoutException(LockTimeout, _stackTraceFetchTimeout);
         Interlocked.Exchange(ref _timeOutExceptionsOnOtherThreads, [.._timeOutExceptionsOnOtherThreads, exception]);
         return exception;
      });

      void UpdateAnyRegisteredTimeoutExceptions()
      {
         // ReSharper disable once InconsistentlySynchronizedField
         if(_timeOutExceptionsOnOtherThreads.Count > 0)
         {
            _timeoutMonitor.Locked(() =>
            {
               var stackTrace = new StackTrace(fNeedFileInfo: true);
               foreach(var exception in _timeOutExceptionsOnOtherThreads)
               {
                  exception.SetBlockingThreadsDisposeStackTrace(stackTrace);
               }

               Interlocked.Exchange(ref _timeOutExceptionsOnOtherThreads, []);
            });
         }
      }

      public void Dispose() => _mutex.Dispose();
   }
}
