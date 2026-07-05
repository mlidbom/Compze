using Compze.Threading.Interprocess.Exceptions;
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
      readonly ThreadLocal<int> _nestingDepth = new();

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

      public ILock TakeLock(CancellationToken cancellationToken = default, LockTimeout? timeout = null) => TryTakeLock(timeout, cancellationToken) ?? throw RegisterTimeoutException();

      static readonly TimeSpan CancellationPollingInterval = TimeSpan.FromSeconds(1);

      public ILock? TryTakeLock(LockTimeout? timeout = null, CancellationToken cancellationToken = default)
      {
         var effectiveTimeout = timeout ?? LockTimeout;

         bool acquired;
         try
         {
            acquired = _mutex.WaitOne(TimeSpan.Zero);
            if(!acquired && timeout != TimeSpan.Zero)
            {
               Interlocked.Increment(ref _contentionCount);

               // Poll so the thread periodically returns to managed code, allowing:
               // 1. A pending Thread.Interrupt to fire as ThreadInterruptedException.
               // 2. A CancellationToken to be checked between iterations.
               // Without this, Mutex.WaitOne on Linux enters an unmanaged wait that is not interruptible.
               var deadline = DateTime.UtcNow + effectiveTimeout.ToTimeSpan();
               while(true)
               {
                  cancellationToken.ThrowIfCancellationRequested();
                  var remaining = deadline - DateTime.UtcNow;
                  if(remaining <= TimeSpan.Zero)
                  {
                     acquired = false;
                     break;
                  }

                  acquired = _mutex.WaitOne(TimeSpan.Min(remaining, CancellationPollingInterval));
                  if(acquired) break;
               }
            }
         }
         catch(AbandonedMutexException)
         {
            _onAbandonedMutex?.Invoke();
            acquired = true;
         }

         if(acquired) _nestingDepth.Value++;
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
         _nestingDepth.Value--;
         _mutex.ReleaseMutex();
      }

      public int ReleaseAllNestingLevels()
      {
         var depth = _nestingDepth.Value;
         UpdateAnyRegisteredTimeoutExceptions();
         for(var i = 0; i < depth; i++)
            _mutex.ReleaseMutex();
         _nestingDepth.Value = 0;
         return depth;
      }

      public void ReacquireToNestingDepth(int depth, LockTimeout? timeout = null)
      {
         for(var i = 0; i < depth; i++)
            TakeLock(timeout: timeout);
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

      public void Dispose()
      {
         _mutex.Dispose();
         _nestingDepth.Dispose();
      }
   }
}
