using Compze.Threading;

namespace Compze.Teventive.MemoryImageStateStore._private.Storage;

///<summary>What a memory image state store says when another memory image holds its storage, such as its folder or its tables: one memory image<br/>
/// at a time uses a store's storage, which the first to load it takes, and holds until its container is disposed.</summary>
static class StorageHeldByAnotherMemoryImage
{
   ///<summary>What the lock of a store's storage is held for, by the memory image that holds it, for the store kept in <paramref name="whereItIsKept"/>.</summary>
   internal static string PurposeOfTheLock(string whereItIsKept) => $"the memory image state store kept in {whereItIsKept}";

   ///<summary>The refusal of a memory image whose store, kept in <paramref name="whereItIsKept"/>, waited <paramref name="waited"/> for its storage,<br/>
   /// which another memory image still holds: <paramref name="holder"/>, as that memory image recorded itself as it took the storage's lock.</summary>
   internal static TimeoutException Refusal(string whereItIsKept, string? holder, WaitTimeout waited) =>
      new($"The memory image state store kept in {whereItIsKept} is used by another memory image{(waited == WaitTimeout.Zero ? "" : $", which this one waited {waited} for to release it")}: "
        + $"its lock is held {holder ?? "by a memory image that took it this instant and hasn't recorded itself yet"}. "
        + "One memory image at a time uses a store's storage: the first to load it holds it until its container is disposed."
        + (waited == WaitTimeout.Zero
              ? $" To have a memory image wait for the holder to release it, as in a hand-over between processes, declare {nameof(MemoryImageStateStoreBuilder.WaitingForAnotherMemoryImageToReleaseTheStorage)}(...) where the store is composed."
              : ""));
}
