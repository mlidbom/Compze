using System;
using System.Threading;
using Compze.Utilities.Functional.ActionFuncHarmonization;
using JetBrains.Annotations;

namespace Compze.Utilities.SystemCE.ThreadingCE;

public class MutexCE
{
   readonly Mutex _mutex;

   MutexCE(string mutexName) =>
      _mutex = Mutex.TryOpenExisting(mutexName, out var mutex)
                  ? mutex
                  : new Mutex(initiallyOwned: false, name: mutexName);

   public void Locked([InstantHandle] Action action) => Locked(action.AsFunc());

   public TResult Locked<TResult>([InstantHandle] Func<TResult> func)
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

   public static MutexCE ForMutexNamed(string name) => new(name);
}
