using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial class LockCE
{
   public static ILock WithDefaultTimeout() => ILock.WithDefaultTimeout();
   public static ILock WithTimeout(TimeSpan timeout) => ILock.WithTimeout(timeout);

   readonly MonitorCE _monitor = new();

   //By creating the locks only once in the constructor usages become zero-allocation operations. By always referencing them by the concrete type inlining remains possible.
   readonly IDisposable _readLock;
   readonly IDisposable _updateLock;

   public TimeSpan Timeout { get; }
   TimeSpan? _stackTraceFetchTimeout;

   internal LockCE(TimeSpan timeout)
   {
      _readLock = new ReadLock(this);
      _stackTraceFetchTimeout = null;
      _updateLock = new UpdateLock(this);
      Timeout = timeout;
   }
}
