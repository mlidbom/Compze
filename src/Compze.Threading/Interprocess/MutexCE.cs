using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using JetBrains.Annotations;

namespace Compze.Threading.Interprocess;

///<summary>
/// <see cref="Mutex"/> behaves differently on windows and other platforms.
/// It also has some surprising API quirks, like occasionally throwing <see cref="AbandonedMutexException"/>> on success, and hidden magic based on the content of the name used.
///
/// This class wraps <see cref="Mutex"/> to eliminate those differences and quirks and exposes only the functionality that works identically on all platform without unpleasant surprises in an easy-to-understand API.
/// </summary>
public class MutexCE : IDisposable
{
   readonly Mutex _mutex;

   MutexCE(string mutexName)
   {
      _mutex = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                  ? WindowsGlobalMutex(mutexName)
                  : NonWindowsGlobalMutex(mutexName);
   }

   ///<summary>
   ///On windows a mutex must be prefixed with Global\ to work across all processes and user login SIDs
   /// And for that to work it must be configured with certain access rules or access will fail with exceptions
   /// </summary>
   [SupportedOSPlatform("windows")]
   static Mutex WindowsGlobalMutex(string mutexName)
   {
      var security = new MutexSecurity();
      security.AddAccessRule(new MutexAccessRule(
                                new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null),
                                MutexRights.FullControl,
                                AccessControlType.Allow));
      return MutexAcl.Create(initiallyOwned: false, mutexName, out _, security);
   }

   static Mutex NonWindowsGlobalMutex(string mutexName) => new(initiallyOwned: false, name: mutexName);

   public void Locked([InstantHandle] Action action, Action? onAbandonedMutex = null) => Locked(action.ToFunc(), onAbandonedMutex);

   public TResult Locked<TResult>([InstantHandle] Func<TResult> func, Action? onAbandonedMutex = null)
   {
      try
      {
         try
         {
            _mutex.WaitOne();
         }
         catch(AbandonedMutexException)
         {
            onAbandonedMutex?.Invoke();
         } // The mutex IS acquired when this exception is thrown. https://learn.microsoft.com/en-us/dotnet/api/System.Threading.AbandonedMutexException

         return func();
      }
      finally
      {
         _mutex.ReleaseMutex();
      }
   }

   ///<summary>Returns a <see cref="MutexCE"/> that synchronises across all processes and user login sessions on the machine.</summary>
   public static MutexCE GlobalNamed(string name)
   {
      if(name.Contains('\\')) throw new ArgumentException("Name must not contain backslashes", nameof(name));
      return new MutexCE($@"Global\{name}");
   }

   ///<summary>Returns a <see cref="MutexCE"/> that synchronises across all processes within a single user login session on the machine.</summary>
   public static MutexCE LocalNamed(string name)
   {
      if(name.Contains('\\')) throw new ArgumentException("Name must not contain backslashes", nameof(name));
      return new MutexCE($@"Local\{name}");
   }

   public void Dispose() => _mutex.Dispose();
}
