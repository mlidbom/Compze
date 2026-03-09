namespace Compze.Threading.ResourceAccess;

public interface IShared<out TShared>
{
   ILock Lock { get; }

   TResult Locked<TResult>(Func<TShared, TResult> func, LockTimeout? timeout = null);

   unit Locked(Action<TShared> action, LockTimeout? timeout = null) => Locked(action.ToFunc(), timeout);
}
