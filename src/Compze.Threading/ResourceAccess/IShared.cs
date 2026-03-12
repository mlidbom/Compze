using Compze.SystemCE;

namespace Compze.Threading.ResourceAccess;

///<summary>Factory for creating <see cref="IShared{TShared}"/> instances that protect a shared object with an <see cref="ILock"/>.</summary>
public interface IShared
{
   ///<summary>Returns a new <see cref="IShared{TShared}"/> that protects <paramref name="shared"/> with <paramref name="lock"/>.</summary>
   public static IShared<TShared> New<TShared>(TShared shared, ILock @lock) =>
      new Shared<TShared>(shared, @lock);

   internal class Shared<TShared>(TShared shared, ILock @lock) : IShared<TShared>
   {
      readonly TShared _shared = shared;
      public ILock Lock { get; } = @lock;

      public TResult Locked<TResult>(Func<TShared, TResult> func, LockTimeout? timeout = null) =>
         Lock.Locked(() => func(_shared), timeout);
   }
}

///<summary>Provides lock-protected access to a shared object of type <typeparamref name="TShared"/>.</summary>
public interface IShared<out TShared>
{
   ///<summary>The underlying lock used to protect access.</summary>
   ILock Lock { get; }

   ///<summary>Acquires the lock, passes the shared object to <paramref name="func"/>, returns the result, then releases the lock.</summary>
   TResult Locked<TResult>(Func<TShared, TResult> func, LockTimeout? timeout = null);

   ///<summary>Acquires the lock, passes the shared object to <paramref name="action"/>, then releases the lock.</summary>
   Unit Locked(Action<TShared> action, LockTimeout? timeout = null) => Locked(action.ToFunc(), timeout);
}
