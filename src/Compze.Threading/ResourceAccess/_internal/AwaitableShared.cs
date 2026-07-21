// ReSharper disable ConvertToPrimaryConstructor

using System.Diagnostics.CodeAnalysis;

namespace Compze.Threading.ResourceAccess._internal;

///<summary>The base implementation every <see cref="IAwaitableShared{TShared}"/> flavor builds on: holds the shared object and<br/>
/// runs each verb inside the <see cref="IAwaitableCriticalSection"/> the flavor supplies.</summary>
class AwaitableShared<TShared> : IAwaitableShared<TShared>
{
   readonly TShared _shared;

   protected AwaitableShared(TShared shared, IAwaitableCriticalSection criticalSection)
   {
      _shared = shared;
      CriticalSection = criticalSection;
   }

   public IAwaitableCriticalSection CriticalSection { get; }

   public TResult Read<TResult>(Func<TShared, TResult> read, CancellationToken cancellationToken = default, LockTimeout? timeout = null) =>
      CriticalSection.Read(() => read(_shared), cancellationToken, timeout);

   public TResult ReadWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> read, CancellationToken cancellationToken = default, WaitTimeout? timeout = null) =>
      CriticalSection.ReadWhen(() => condition(_shared), () => read(_shared), cancellationToken, waitTimeout: timeout);

   public TResult Update<TResult>(Func<TShared, TResult> update, CancellationToken cancellationToken = default, LockTimeout? timeout = null) =>
      CriticalSection.Update(() => update(_shared), cancellationToken, timeout);

   public TResult UpdateWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> update, CancellationToken cancellationToken = default, WaitTimeout? timeout = null) =>
      CriticalSection.UpdateWhen(() => condition(_shared), () => update(_shared), cancellationToken, waitTimeout: timeout);

   public bool TryReadWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> read, [MaybeNullWhen(false)] out TResult result, CancellationToken cancellationToken = default, WaitTimeout? timeout = null) =>
      CriticalSection.TryReadWhen(() => condition(_shared), () => read(_shared), out result, cancellationToken, waitTimeout: timeout);

   public bool TryUpdateWhen(Func<TShared, bool> condition, Action<TShared> update, CancellationToken cancellationToken = default, WaitTimeout? timeout = null) =>
      CriticalSection.TryUpdateWhen(() => condition(_shared), () => update(_shared), cancellationToken, waitTimeout: timeout);

   public bool TryAwait(Func<TShared, bool> condition, CancellationToken cancellationToken = default, WaitTimeout? timeout = null) =>
      CriticalSection.TryAwait(() => condition(_shared), cancellationToken, waitTimeout: timeout);
}
