using Compze.SystemCE;

namespace Compze.Threading.ResourceAccess;

///<summary>Contains the internal base implementation for <see cref="IShared{TShared}"/>.</summary>
public interface IShared
{
   internal class Shared<TShared>(TShared shared, ICriticalSection criticalSection) : IShared<TShared>
   {
      readonly TShared _shared = shared;
      public ICriticalSection CriticalSection { get; } = criticalSection;

      public TResult Locked<TResult>(Func<TShared, TResult> func, CancellationToken cancellationToken = default, LockTimeout? timeout = null) =>
         CriticalSection.Locked(() => func(_shared), cancellationToken, timeout);
   }
}

///<summary>Provides lock-protected access to a shared object of type <typeparamref name="TShared"/>.</summary>
public interface IShared<out TShared>
{
   ///<summary>The underlying critical section used to protect access.</summary>
   ICriticalSection CriticalSection { get; }

   ///<summary>Acquires the lock, passes the shared object to <paramref name="func"/>, returns the result, then releases the lock.</summary>
   TResult Locked<TResult>(Func<TShared, TResult> func, CancellationToken cancellationToken = default, LockTimeout? timeout = null);

   ///<summary>Acquires the lock, passes the shared object to <paramref name="action"/>, then releases the lock.</summary>
   Unit Locked(Action<TShared> action, CancellationToken cancellationToken = default, LockTimeout? timeout = null) => Locked(action.ToFunc(), cancellationToken, timeout);
}
