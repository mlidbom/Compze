using Compze.SystemCE;

namespace Compze.Threading.ResourceAccess;

public interface IShared
{
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

public interface IShared<out TShared>
{
   ILock Lock { get; }

   TResult Locked<TResult>(Func<TShared, TResult> func, LockTimeout? timeout = null);

   Unit Locked(Action<TShared> action, LockTimeout? timeout = null) => Locked(action.ToFunc(), timeout);
}
