using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using Compze.SystemCE.CollectionsCE.GenericCE;
using Compze.SystemCE.ThreadingCE.ResourceAccess;
using JetBrains.Annotations;

namespace Compze.SystemCE.ThreadingCE;

class MachineWideSingleThreaded
{
   static readonly IThreadShared<Dictionary<string, Mutex>> Cache = ThreadShared.WithDefaultTimeout(new Dictionary<string, Mutex>());

   readonly Mutex _mutex;
   MachineWideSingleThreaded(string lockId)
   {
      var lockId1 = $@"Global\{lockId}";

      _mutex = Cache.Update(cache => cache.GetOrAdd(lockId1,
                                                    () =>
                                                    {
                                                       try
                                                       {
                                                          var existing = Mutex.OpenExisting(lockId1);
                                                          return existing;
                                                       }
                                                       catch
                                                       {
                                                          var mutex = new Mutex(initiallyOwned: false, name: lockId1);

                                                          if(OperatingSystem.IsWindows())
                                                          {
                                                             var mutexSecurity = new MutexSecurity();
                                                             mutexSecurity.AddAccessRule(new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                                                                                                             MutexRights.FullControl,
                                                                                                             AccessControlType.Allow));
                                                             mutex.SetAccessControl(mutexSecurity);
                                                          }

                                                          return mutex;
                                                       }
                                                    }));
   }

   internal void Execute([InstantHandle] Action action)
   {
      try
      {
         _mutex.WaitOne();
         action();
      }
      finally
      {
         _mutex.ReleaseMutex();
      }
   }

   internal TResult Execute<TResult>([InstantHandle] Func<TResult> func)
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
   static MachineWideSingleThreaded For(Type synchronized) => new($"{nameof(MachineWideSingleThreaded)}_{synchronized.AssemblyQualifiedName}");
}