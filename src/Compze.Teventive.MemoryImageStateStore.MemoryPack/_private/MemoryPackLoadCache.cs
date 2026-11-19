using System.Reflection;
using Compze.Contracts;
using Compze.Teventive.MemoryImageStateStore._internal;
using MemoryPack;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.MemoryImageStateStore.MemoryPack._private;

///<summary>The load cache <see cref="TaggregateFilesBuilderMemoryPackCE.StartingFromAMemoryPackCache"/> plugs in: each class's snapshots, kept with<br/>
/// MemoryPack, which reads them back far faster than the JSON files they come from.</summary>
sealed class MemoryPackLoadCache : ITaggregateFilesLoadCache
{
   static readonly MethodInfo IsRegistered = typeof(MemoryPackFormatterProvider).GetMethod(nameof(MemoryPackFormatterProvider.IsRegistered))!;

   public void AssertCanHold(Type snapshotType) =>
      State.Assert(MemoryPackSerializes(snapshotType),
                   () => $"The memory image state store's MemoryPack cache can't hold snapshots of {snapshotType.FullName}: MemoryPack has no formatter for the type. Make it [MemoryPackable], or register a formatter for it.");

   public ICachedFilesOfAClass Open(byte[]? written, Type snapshotType) => MemoryPackCachedFilesOfAClass.From(written, snapshotType);

   ///<summary>Whether MemoryPack serializes <paramref name="type"/>: a <c>[MemoryPackable]</c> type registers its own formatter, and<br/>
   /// any other type needs one registered.</summary>
   static bool MemoryPackSerializes(Type type) =>
      typeof(IMemoryPackFormatterRegister).IsAssignableFrom(type) || (bool)IsRegistered.MakeGenericMethod(type).Invoke(null, null)!;
}
