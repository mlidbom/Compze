using System;
using System.Threading;
using Compze.Utilities.SystemCE;
using JetBrains.Annotations;

namespace Compze.Utilities.Testing.DbPool.SystemCE.ThreadingCE;

class MachineWideSingleThreaded
{
   readonly Mutex _mutex;

   MachineWideSingleThreaded(string lockId)
   {
      var globalMutexId = $@"Global\{lockId}";

      _mutex = Mutex.TryOpenExisting(globalMutexId, out var mutex) ? mutex : new Mutex(initiallyOwned: false, name: globalMutexId);
   }

   internal void ExecuteWithLock([InstantHandle] Action action) => ExecuteWithLock(action.AsUnitFunc());

   internal TResult ExecuteWithLock<TResult>([InstantHandle] Func<TResult> func)
   {
      try
      {
         _mutex.WaitOne();
         return func();
      }
      finally
      {
         _mutex.ReleaseMutex();
      }
   }

   internal static MachineWideSingleThreaded For(string name) => new(name);
}

class MutexCE {}
