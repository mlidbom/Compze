using System;
using System.Threading;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;
using JetBrains.Annotations;

namespace Compze.Utilities.Testing.DbPool.SystemCE.ThreadingCE;

class MutexCE
{
   readonly Mutex _mutex;

   MutexCE(string mutexName) =>
      _mutex = Mutex.TryOpenExisting(mutexName, out var mutex)
                  ? mutex
                  : new Mutex(initiallyOwned: false, name: mutexName);

   internal void ExecuteWithLock([InstantHandle] Action action) => ExecuteWithLock(action.AsFunc());

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

   internal static MutexCE ForMutexNamed(string name) => new(name);
}
