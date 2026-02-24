using System;
using System.Threading;
using Compze.Utilities.Functional;
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
         try
         {
            _mutex.WaitOne();
         }
         catch(AbandonedMutexException) { } // The mutex IS acquired when this exception is thrown. https://learn.microsoft.com/en-us/dotnet/api/System.Threading.AbandonedMutexException

         return func();
      }
      finally
      {
         _mutex.ReleaseMutex();
      }
   }

   public static MutexCE ForMutexNamed(string name) => new(name);
}
