using JetBrains.Annotations;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Compze.Threading;

public class MutexCE
{
   readonly Mutex _mutex;

   MutexCE(string mutexName) => _mutex = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                            ? WindowsGlobalMutex(mutexName)
                                            : NonWindowsGlobalMutex(mutexName);

   [SupportedOSPlatform("windows")]
   static Mutex WindowsGlobalMutex(string mutexName)
   {
      // Global\ mutexes need an explicit ACL granting cross-session access.
      // Without this, Session 0 (services) and interactive sessions can't share the mutex
      // even when running under the same user account, because they have different logon SIDs.
      var security = new MutexSecurity();
      security.AddAccessRule(new MutexAccessRule(
                                new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null),
                                MutexRights.FullControl,
                                AccessControlType.Allow));
      return MutexAcl.Create(initiallyOwned: false, $@"Global\{mutexName}", out _, security);
   }

   static Mutex NonWindowsGlobalMutex(string mutexName) => new(initiallyOwned: false, name: mutexName);

   public TResult Locked<TResult>([InstantHandle] Func<TResult> func)
   {
      try
      {
         try
         {
            _mutex.WaitOne();
         }
         catch(AbandonedMutexException) {} // The mutex IS acquired when this exception is thrown. https://learn.microsoft.com/en-us/dotnet/api/System.Threading.AbandonedMutexException

         return func();
      }
      finally
      {
         _mutex.ReleaseMutex();
      }
   }

   ///<summary>Returns a mutex that synchronises across all processes and user login sessions on the machine.
   /// On Windows, uses a Global\ kernel mutex with an ACL granting cross-session access.
   /// On other platforms, uses a plain named mutex (cross-session isolation doesn't exist).</summary>
   public static MutexCE GlobalNamed(string name) => new(name);
}
