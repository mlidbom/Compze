using JetBrains.Annotations;

namespace Compze.Threading.ResourceAccess._internal;

///<summary>The base implementation every <see cref="IShared{TShared}"/> flavor builds on: holds the shared object and runs<br/>
/// <see cref="Locked{TResult}"/> inside the <see cref="ICriticalSection"/> the flavor supplies.</summary>
class Shared<TShared>(TShared shared, ICriticalSection criticalSection) : IShared<TShared>
{
   readonly TShared _shared = shared;
   public ICriticalSection CriticalSection { get; } = criticalSection;

   public TResult Locked<TResult>([InstantHandle]Func<TShared, TResult> func, CancellationToken cancellationToken = default, LockTimeout? timeout = null) =>
      CriticalSection.Locked(() => func(_shared), cancellationToken, timeout);
}
