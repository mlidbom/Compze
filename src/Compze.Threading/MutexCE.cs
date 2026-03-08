using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using JetBrains.Annotations;

namespace Compze.Threading;

public class MutexCE
{
   readonly Mutex _mutex;

   MutexCE(string mutexName) =>
      _mutex = Mutex.TryOpenExisting(mutexName, out var mutex)
                  ? mutex
                  : new Mutex(initiallyOwned: false, name: mutexName);

   [SupportedOSPlatform("windows")]
   MutexCE(string mutexName, MutexSecurity security) =>
      _mutex = MutexAcl.Create(initiallyOwned: false, mutexName, out _, security);

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
   public static MutexCE GlobalNamed(string name) =>
      RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
         ? CreateWindowsGlobalMutex(name)
         : new(name);

   [SupportedOSPlatform("windows")]
   static MutexCE CreateWindowsGlobalMutex(string name)
   {
      // Global\ mutexes need an explicit ACL granting cross-session access.
      // Without this, Session 0 (services) and interactive sessions can't share the mutex
      // even when running under the same user account, because they have different logon SIDs.
      var security = new MutexSecurity();
      security.AddAccessRule(new MutexAccessRule(
                                new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null),
                                MutexRights.FullControl,
                                AccessControlType.Allow));

      return new($@"Global\{name}", security);
   }
}
