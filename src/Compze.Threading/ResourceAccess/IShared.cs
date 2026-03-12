using Compze.SystemCE;

namespace Compze.Threading.ResourceAccess;

///<summary>Factory for creating <see cref="IShared{TShared}"/> instances that protect a shared object with an <see cref="ICriticalSection"/>.</summary>
public interface IShared
{
   ///<summary>Returns a new <see cref="IShared{TShared}"/> that protects <paramref name="shared"/> with <paramref name="criticalSection"/>.</summary>
   public static IShared<TShared> New<TShared>(TShared shared, ICriticalSection criticalSection) =>
      new Shared<TShared>(shared, criticalSection);

   internal class Shared<TShared>(TShared shared, ICriticalSection criticalSection) : IShared<TShared>
   {
      readonly TShared _shared = shared;
      public ICriticalSection CriticalSection { get; } = criticalSection;

      public TResult Locked<TResult>(Func<TShared, TResult> func, LockTimeout? timeout = null) =>
         CriticalSection.Locked(() => func(_shared), timeout);
   }
}

///<summary>Provides lock-protected access to a shared object of type <typeparamref name="TShared"/>.</summary>
public interface IShared<out TShared>
{
   ///<summary>The underlying critical section used to protect access.</summary>
   ICriticalSection CriticalSection { get; }

   ///<summary>Acquires the lock, passes the shared object to <paramref name="func"/>, returns the result, then releases the lock.</summary>
   TResult Locked<TResult>(Func<TShared, TResult> func, LockTimeout? timeout = null);

   ///<summary>Acquires the lock, passes the shared object to <paramref name="action"/>, then releases the lock.</summary>
   Unit Locked(Action<TShared> action, LockTimeout? timeout = null) => Locked(action.ToFunc(), timeout);
}
