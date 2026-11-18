# Public API changes of the taggregate-store-spike branch

> Written at the start of the end review (2026-11-18), on the spike at 46cc881a6: some names and API here have changed
> since, by its decisions; see [../the-naming-pass.md](../the-naming-pass.md) and
> [end-review-consolidated.md](end-review-consolidated.md).

Only what changed; no opinions.

- **Old side:** the merge-base of `taggregate-store-spike` with `main`, commit `e29fd6673`, built in the worktree `C:\Dev\Compze.worktrees\api-base-e29fd66`: `src\<Project>\bin\Debug\net11.0\<Project>.dll`.
- **New side:** branch `taggregate-store-spike`, commit `46cc881a6`, built in `C:\Dev\Compze`: `src\<Project>\bin\Debug\net11.0\<Project>.dll`.
- **Produced with:** the sherlock MCP's `compare_api_surface` (`projection: "full"`, `maxItems: 300`, left = old, right = new), one call per assembly; every change it reported is listed below. "(breaking)" is the tool's own flag. For every added type, the public and protected members were listed with sherlock's `get_type_members` on the new assembly (members that are `internal`, `private` or `private protected` are not listed). Nested types are written `Outer.Inner`. Within a section, entries are grouped by namespace. Compiler-generated reasons such as `TeventApplyingRoot`2<!0,!1>` are the tool's text, unchanged.
- **Assemblies with no public changes (checked earlier, not listed):** Compze.DependencyInjection, Compze.Serialization.Newtonsoft and the four SQL plugs.
- **New assemblies with no old side:** Compze.Teventive.StateStore and Compze.Teventive.StateStore.MemoryPack, listed in their sections at the end by `get_types_from_assembly` and `get_type_members` (sherlock loaded both; the source was not needed).

## Compze.Teventive

Old: `C:\Dev\Compze.worktrees\api-base-e29fd66\src\Compze.Teventive\bin\Debug\net11.0\Compze.Teventive.dll` ; New: `C:\Dev\Compze\src\Compze.Teventive\bin\Debug\net11.0\Compze.Teventive.dll`

Counts reported by the tool: types added 18, types removed 2, types changed 20, members added 16, members removed 16, members changed 0, breaking 25.

### Compze.Teventive - Added types

**Namespace `Compze.Teventive`**

- `public interface Compze.Teventive.IMutableTaggregateTeventWithIdentity`
  - `void SetTessageIdInternal(TessageId tessageId)`
- `public interface Compze.Teventive.ITeventRouted`
  - `TeventRouting TeventAppliers { get; }`
  - `TeventRouting TeventHandlers { get; }`
- `public interface Compze.Teventive.ITeventRoutedMember<TTevent>`
  - `void DispatchInternal(IPublisherTevent<ITevent> wrapped)`
- `public abstract class Compze.Teventive.TeventRouting`
  - (no public or protected members)
- `public sealed class Compze.Teventive.TeventRouting<TInstance, TTevent>`
  - `public TeventRouting<TSubclass, TTevent> Extend<TSubclass>(Action<Declaration<TSubclass, TTevent>> declare)`
- `public sealed class Compze.Teventive.TeventRouting<TInstance, TTevent>.Declaration`
  - `public Declaration<TInstance, TTevent> Component<TComponentTevent>(Func<TInstance, ITeventRoutedMember<TComponentTevent>> component)`
  - `public Declaration<TInstance, TTevent> Entities<TEntityTevent>(Func<TInstance, ITeventRoutedMember<TEntityTevent>> entities)`
  - `public Declaration<TInstance, TTevent> For<THandledTevent>(Action<TInstance, THandledTevent> handler)`
  - `public Declaration<TInstance, TTevent> IgnoreUnhandled<TIgnoredTevent>()`
  - `public Declaration<TInstance, TTevent> SharedTomponent<TOwnerTevent, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent>(SharedTomponentSlotOfAClass<TInstance, TOwnerTevent, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent> slot)`

**Namespace `Compze.Teventive.Taggregates`**

- `public interface Compze.Teventive.Taggregates.ISnapshotable<TSelf, TSnapshot>`
  - `void RestoreFrom(TSnapshot snapshot)`
  - `TSnapshot TakeSnapshot()`
- `public static class Compze.Teventive.Taggregates.SnapshotRoundTrip`
  - `public static void AssertLosesNothing<TTaggregate, TSnapshot>(TTaggregate taggregate, Func<ISnapshotable<TTaggregate, TSnapshot>> createEmpty, Func<TSnapshot, TSnapshot, bool> snapshotsAreEqual = null)`
  - `public static IReadOnlyList<string> DifferencesOf<TTaggregate, TSnapshot>(TTaggregate taggregate, Func<ISnapshotable<TTaggregate, TSnapshot>> createEmpty, Func<TSnapshot, TSnapshot, bool> snapshotsAreEqual = null)`

**Namespace `Compze.Teventive.Taggregates.BaseClasses`**

- `public abstract class Compze.Teventive.Taggregates.BaseClasses.TeventApplyingComponent<TComponent, TComponentTevent>`
  - `protected static TeventRouting<TComponent, TComponentTevent> DeclareAppliers(Action<Declaration<TComponent, TComponentTevent>> declare)`
- `public abstract class Compze.Teventive.Taggregates.BaseClasses.TeventApplyingRoot<TRoot, TTevent>`
  - `protected virtual void AssertInvariantsAreMet()`
  - `protected static TeventRouting<TRoot, TTevent> DeclareAppliers(Action<Declaration<TRoot, TTevent>> declare)`

**Namespace `Compze.Teventive.Taggregates.BaseClasses.Shared`**

- `public struct Compze.Teventive.Taggregates.BaseClasses.Shared.SharedTomponentSlotInAnOwner<TTomponentTevent>`
  - (no public or protected members)
- `public sealed class Compze.Teventive.Taggregates.BaseClasses.Shared.SharedTomponentSlotOfAClass<TOwner, TOwnerTevent, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent>`
  - `public SharedTomponentSlotOfAClass(Type adoptingWrapperTeventImplementation, Func<TOwner, ISharedTomponentInternals<TTomponentTevent>> tomponentOf)`
  - `public SharedTomponentSlotInAnOwner<TTomponentTevent> In(TOwner owner)`

**Namespace `Compze.Teventive.Taggregates.Tevents`**

- `public interface Compze.Teventive.Taggregates.Tevents.ITaggregateTeventWithIdentity`
  - (no public or protected members)
- `public abstract class Compze.Teventive.Taggregates.Tevents.TaggregateTeventWithIdentity`
  - `public sealed TessageId Id { get; set; }`

**Namespace `Compze.Teventive.TaggregateStores`**

- `public interface Compze.Teventive.TaggregateStores.ITaggregateReference<TTaggregate>`
  - `TTaggregate Current { get; }`
  - `TaggregateId Id { get; }`
  - `TTaggregate ForUpdate()`
- `public interface Compze.Teventive.TaggregateStores.ITaggregateStoreBulkReader`
  - `IReadOnlyCollection<ITaggregateReference<TTaggregate>> All<TTaggregate>()`
- `public interface Compze.Teventive.TaggregateStores.ITaggregateStoreReader`
  - `TTaggregate Get<TTaggregate>(TaggregateId id)`
  - `ITaggregateReference<TTaggregate> Reference<TTaggregate>(TaggregateId id)`
  - `bool TryGet<TTaggregate>(TaggregateId id, out TTaggregate taggregate)`
- `public interface Compze.Teventive.TaggregateStores.ITaggregateStoreUpdater`
  - `TTaggregate GetForUpdate<TTaggregate>(TaggregateId id)`
  - `void Save<TTaggregate>(TTaggregate taggregate)`
  - `bool TryGetForUpdate<TTaggregate>(TaggregateId id, out TTaggregate taggregate)`

### Compze.Teventive - Removed types

**Namespace `Compze.Teventive.Taggregates.BaseClasses.Shared`**

- `public interface Compze.Teventive.Taggregates.BaseClasses.Shared.ISharedTomponentSlot<TTomponentTevent>` (breaking)
- `public sealed class Compze.Teventive.Taggregates.BaseClasses.Shared.SharedTomponentSlot<TOwnerTevent, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent>` (breaking)

### Compze.Teventive - Changed types

**Namespace `Compze.Teventive`**

- `public interface Compze.Teventive.IMutableTaggregateTevent` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IAtLeastOnceTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IAtMostOnceTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IExactlyOnceTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IExactlyOnceTevent` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IMustBeHandledTransactionally` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IMustBeSentAndHandledTransactionally` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IMustBeSentTransactionally` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IRemotableTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IRemotableTevent` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.ITessageWithIdentity` (breaking)

**Namespace `Compze.Teventive.Taggregates.BaseClasses`**

- `public interface Compze.Teventive.Taggregates.BaseClasses.ITeventiveInternals<TTevent, TTeventImplementation>`
  - `now implements Compze.Teventive.ITeventRouted`
- `public class Compze.Teventive.Taggregates.BaseClasses.Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>`
  - `` now derives from Compze.Teventive.Taggregates.BaseClasses.TeventApplyingRoot`2<!0,!1> ``
  - `now implements Compze.Teventive.ITeventRouted`
- `public abstract class Compze.Teventive.Taggregates.BaseClasses.Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.Component<TComponent, TComponentTevent, TComponentTeventImplementation>`
  - `` now derives from Compze.Teventive.Taggregates.BaseClasses.TeventApplyingComponent`2<!5,!6> ``
  - `now implements Compze.Teventive.ITeventRouted`
  - `` now implements Compze.Teventive.ITeventRoutedMember`1<!6> ``
- `public abstract class Compze.Teventive.Taggregates.BaseClasses.Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.Entity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityTeventIdGetterSetter>`
  - `` now derives from Compze.Teventive.Taggregates.BaseClasses.TeventApplyingComponent`2<!5,!7> ``
  - `now implements Compze.Teventive.ITeventRouted`
  - `` now implements Compze.Teventive.ITeventRoutedMember`1<!7> ``
- `public abstract class Compze.Teventive.Taggregates.BaseClasses.Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.RemovableEntity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>`
  - `` now derives from Compze.Teventive.Taggregates.BaseClasses.TeventApplyingComponent`2<!5,!7> ``
  - `now implements Compze.Teventive.ITeventRouted`
  - `` now implements Compze.Teventive.ITeventRoutedMember`1<!7> ``
- `public abstract class Compze.Teventive.Taggregates.BaseClasses.Tentity<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityId, TTentityTeventImplementation, TTentityTevent, TTentityCreatedTevent, TTentityTeventIdGetterSetter>`
  - `` now derives from Compze.Teventive.Taggregates.BaseClasses.TeventApplyingComponent`2<!3,!6> ``
  - `now implements Compze.Teventive.ITeventRouted`
  - `` now implements Compze.Teventive.ITeventRoutedMember`1<!6> ``
- `public interface Compze.Teventive.Taggregates.BaseClasses.Tentity<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityId, TTentityTeventImplementation, TTentityTevent, TTentityCreatedTevent, TTentityTeventIdGetterSetter>.ICollectionManager`
  - `` now implements Compze.Teventive.ITeventRoutedMember`1<!6> ``
- `public abstract class Compze.Teventive.Taggregates.BaseClasses.TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>`
  - `` now derives from Compze.Teventive.Taggregates.BaseClasses.TeventApplyingComponent`2<!3,!4> ``
  - `now implements Compze.Teventive.ITeventRouted`
  - `` now implements Compze.Teventive.ITeventRoutedMember`1<!4> ``
- `public abstract class Compze.Teventive.Taggregates.BaseClasses.TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.Component<TEcComponent, TEcComponentTeventImplementation, TEcComponentTevent>`
  - `` now derives from Compze.Teventive.Taggregates.BaseClasses.TeventApplyingComponent`2<!6,!8> ``
  - `now implements Compze.Teventive.ITeventRouted`
  - `` now implements Compze.Teventive.ITeventRoutedMember`1<!8> ``
- `public abstract class Compze.Teventive.Taggregates.BaseClasses.TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.Entity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityTeventIdGetterSetter>`
  - `` now derives from Compze.Teventive.Taggregates.BaseClasses.TeventApplyingComponent`2<!6,!8> ``
  - `now implements Compze.Teventive.ITeventRouted`
  - `` now implements Compze.Teventive.ITeventRoutedMember`1<!8> ``
- `public abstract class Compze.Teventive.Taggregates.BaseClasses.TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.RemovableEntity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>`
  - `` now derives from Compze.Teventive.Taggregates.BaseClasses.TeventApplyingComponent`2<!6,!8> ``
  - `now implements Compze.Teventive.ITeventRouted`
  - `` now implements Compze.Teventive.ITeventRoutedMember`1<!8> ``
- `public abstract class Compze.Teventive.Taggregates.BaseClasses.TeventiveRemovableEntity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>`
  - `` now derives from Compze.Teventive.Taggregates.BaseClasses.TeventApplyingComponent`2<!3,!5> ``
  - `now implements Compze.Teventive.ITeventRouted`
  - `` now implements Compze.Teventive.ITeventRoutedMember`1<!5> ``

**Namespace `Compze.Teventive.Taggregates.BaseClasses.Shared`**

- `public abstract class Compze.Teventive.Taggregates.BaseClasses.Shared.SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>`
  - `now implements Compze.Teventive.ITeventRouted`
- `public sealed class Compze.Teventive.Taggregates.BaseClasses.Shared.SharedTentityCollection<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>` (breaking)
  - `became sealed` (breaking)
  - `now implements Compze.Teventive.ITeventRouted`
- `public abstract class Compze.Teventive.Taggregates.BaseClasses.Shared.SharedTomponent<TTomponentTevent>`
  - `now implements Compze.Teventive.ITeventRouted`

**Namespace `Compze.Teventive.Taggregates.Tevents`**

- `public interface Compze.Teventive.Taggregates.Tevents.ITaggregateCreatedTevent` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IAtLeastOnceTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IAtMostOnceTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IExactlyOnceTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IExactlyOnceTevent` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IMustBeHandledTransactionally` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IMustBeSentAndHandledTransactionally` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IMustBeSentTransactionally` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IRemotableTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IRemotableTevent` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.ITessageWithIdentity` (breaking)
- `public interface Compze.Teventive.Taggregates.Tevents.ITaggregateDeletedTevent` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IAtLeastOnceTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IAtMostOnceTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IExactlyOnceTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IExactlyOnceTevent` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IMustBeHandledTransactionally` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IMustBeSentAndHandledTransactionally` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IMustBeSentTransactionally` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IRemotableTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IRemotableTevent` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.ITessageWithIdentity` (breaking)
- `public interface Compze.Teventive.Taggregates.Tevents.ITaggregateTevent` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IAtLeastOnceTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IAtMostOnceTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IExactlyOnceTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IExactlyOnceTevent` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IMustBeHandledTransactionally` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IMustBeSentAndHandledTransactionally` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IMustBeSentTransactionally` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IRemotableTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IRemotableTevent` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.ITessageWithIdentity` (breaking)
- `public abstract class Compze.Teventive.Taggregates.Tevents.TaggregateTevent` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IAtLeastOnceTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IAtMostOnceTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IExactlyOnceTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IExactlyOnceTevent` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IMustBeHandledTransactionally` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IMustBeSentAndHandledTransactionally` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IMustBeSentTransactionally` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IRemotableTessage` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.IRemotableTevent` (breaking)
  - `no longer implements Compze.Tessaging.TessageTypes.ITessageWithIdentity` (breaking)

### Compze.Teventive - Added members

**Namespace `Compze.Teventive.Taggregates.BaseClasses`**

- `IEntityCollectionManager<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent>`
  - method: `TEntity AddRestored(TEntityId id)` (breaking) - abstract member added to an interface; implementers must add it
- `Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>`
  - constructor: `` public Taggregate`5() ``
  - method: `protected void AssertCanBeChanged()`
  - method: `protected static TeventRouting<TTaggregate, TTaggregateTevent> DeclareHandlers(Action<Declaration<TTaggregate, TTaggregateTevent>> declare)`
  - method: `protected void RestoreIdentity(TaggregateId id)`
- `TaggregateIdentifyingTevent`
  - method: `public static ITaggregateTevent<ITaggregateTeventWithIdentity> WrapIn(Type wrapperTeventImplementation, ITaggregateTeventWithIdentity tevent)`
- `Tentity<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityId, TTentityTeventImplementation, TTentityTevent, TTentityCreatedTevent, TTentityTeventIdGetterSetter>`
  - constructor: `` protected Tentity`9(TParent taggregate) ``
  - method: `protected static TeventRouting<TTentity, TTentityTevent> DeclareAppliers(Action<Declaration<TTentity, TTentityTevent>> declare)`
- `TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>`
  - constructor: `` protected TeventiveComponent`6(TParent parent) ``
- `TeventiveRemovableEntity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>`
  - method: `protected static TeventRouting<TEntity, TEntityTevent> DeclareAppliers(Action<Declaration<TEntity, TEntityTevent>> declare)`

**Namespace `Compze.Teventive.Taggregates.BaseClasses.Shared`**

- `ISharedTomponentInternals<TTomponentTevent>`
  - method: `void ApplyAdoptedTeventInternal(IPublisherTevent<TTomponentTevent> adopted)` (breaking) - abstract member added to an interface; implementers must add it
- `SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>`
  - method: `protected static TeventRouting<TTentity, TTentityTevent> DeclareAppliers(Action<Declaration<TTentity, TTentityTevent>> declare)`
- `SharedTentityCollection<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>`
  - constructor: `` public SharedTentityCollection`4(SharedTomponentSlotInAnOwner<TTentityTevent> slot) ``
  - method: `public TTentity AddRestored(TTentityId id)`
- `SharedTomponent<TTomponentTevent>`
  - constructor: `` protected SharedTomponent`1(SharedTomponentSlotInAnOwner<TTomponentTevent> slot) ``
  - method: `protected static TeventRouting<TTomponent, TTomponentTevent> DeclareAppliers<TTomponent>(Action<Declaration<TTomponent, TTomponentTevent>> declare)`

### Compze.Teventive - Removed members

**Namespace `Compze.Teventive`**

- `IMutableTaggregateTevent`
  - method: `void SetTessageIdInternal(TessageId tessageId)` (breaking) - member removed or no longer visible

**Namespace `Compze.Teventive.Taggregates.BaseClasses`**

- `ITeventiveInternals<TTevent, TTeventImplementation>`
  - method: `void ApplyTeventInternal(TTevent tevent)` (breaking) - member removed or no longer visible
  - method: `ITeventSubscriber<TTevent> RegisterTeventAppliersInternal()` (breaking) - member removed or no longer visible
- `Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>`
  - constructor: `` protected Taggregate`5(TeventDispatcherConfig teventAppliersDispatcherConfig = null) `` (breaking) - member removed or no longer visible
  - method: `protected virtual void AssertInvariantsAreMet()` - no longer declared here but still inherited from a base type; callers are unaffected
  - method: `protected ITeventSubscriber<TTaggregateTevent> RegisterTeventAppliers()` (breaking) - member removed or no longer visible
  - method: `protected ITeventSubscriber<TTaggregateTevent> RegisterTeventHandlers()` (breaking) - member removed or no longer visible
- `Tentity<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityId, TTentityTeventImplementation, TTentityTevent, TTentityCreatedTevent, TTentityTeventIdGetterSetter>`
  - constructor: `` protected Tentity`9(TParent taggregate, TeventDispatcherConfig teventAppliersDispatcherConfig = null) `` (breaking) - member removed or no longer visible
- `TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>`
  - constructor: `` protected TeventiveComponent`6(TParent parent, bool registerTeventAppliers = true, TeventDispatcherConfig teventAppliersDispatcherConfig = null) `` (breaking) - member removed or no longer visible
  - method: `protected ITeventSubscriber<TComponentTevent> RegisterTeventAppliers()` (breaking) - member removed or no longer visible

**Namespace `Compze.Teventive.Taggregates.BaseClasses.Shared`**

- `ISharedTomponentInternals<TTomponentTevent>`
  - method: `void ApplyTeventInternal(TTomponentTevent tevent)` (breaking) - member removed or no longer visible
- `SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>`
  - method: `protected ITeventSubscriber<TTentityTevent> RegisterTeventAppliers()` (breaking) - member removed or no longer visible
- `SharedTentityCollection<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>`
  - constructor: `` public SharedTentityCollection`4(ISharedTomponentSlot<TTentityTevent> slot) `` (breaking) - member removed or no longer visible
- `SharedTomponent<TTomponentTevent>`
  - constructor: `` protected SharedTomponent`1(ISharedTomponentSlot<TTomponentTevent> slot, TeventDispatcherConfig teventAppliersDispatcherConfig = null) `` (breaking) - member removed or no longer visible
  - method: `protected ITeventSubscriber<TTomponentTevent> RegisterTeventAppliers()` (breaking) - member removed or no longer visible

**Namespace `Compze.Teventive.Taggregates.Tevents`**

- `TaggregateTevent`
  - property: `public sealed TessageId Id { get; set; }` (breaking) - member removed or no longer visible

### Compze.Teventive - Changed members

(none)


## Compze.Tessaging

Old: `C:\Dev\Compze.worktrees\api-base-e29fd66\src\Compze.Tessaging\bin\Debug\net11.0\Compze.Tessaging.dll` ; New: `C:\Dev\Compze\src\Compze.Tessaging\bin\Debug\net11.0\Compze.Tessaging.dll`

Counts reported by the tool: types added 1, types removed 0, types changed 0, members added 17, members removed 0, members changed 0, breaking 4.

### Compze.Tessaging - Added types

**Namespace `Compze.Tessaging.TessageBus`**

- `public interface Compze.Tessaging.TessageBus.ITeventObservation`
  - `IDisposable ObserveTevents(Action<TeventObservationRegistrar> observe)`

### Compze.Tessaging - Removed types

(none)

### Compze.Tessaging - Changed types

(none)

### Compze.Tessaging - Added members

**Namespace `Compze.Tessaging.Engine`**

- `ILocalTessagingEngine`
  - method: `Task AwaitNoTessagesInFlightAsync()` (breaking) - abstract member added to an interface; implementers must add it

**Namespace `Compze.Tessaging.TessageBus`**

- `ITeventObservationRegistrar`
  - method: `ITeventObservationRegistrar AtRuntime<TTevent>()` (breaking) - abstract member added to an interface; implementers must add it
  - method: `ITeventObservationRegistrar ForEachUnitOfWork<TTevent>(Action<IReadOnlyList<TTevent>, IScopeResolver> observer)` (breaking) - abstract member added to an interface; implementers must add it
  - method: `ITeventObservationRegistrar ForEachUnitOfWork<TTevent>(Func<IReadOnlyList<TTevent>, IScopeResolver, Task> observer)` (breaking) - abstract member added to an interface; implementers must add it
- `ITeventObservationRegistrarCE`
  - method: `public static ITeventObservationRegistrar ForEachUnitOfWork<TTevent, TDependency1>(ITeventObservationRegistrar this, Action<IReadOnlyList<TTevent>, TDependency1> observer)`
  - method: `public static ITeventObservationRegistrar ForEachUnitOfWork<TTevent, TDependency1>(ITeventObservationRegistrar this, Func<IReadOnlyList<TTevent>, TDependency1, Task> observer)`
  - method: `public static ITeventObservationRegistrar ForEachUnitOfWork<TTevent>(ITeventObservationRegistrar this, Action<IReadOnlyList<TTevent>> observer)`
  - method: `public static ITeventObservationRegistrar ForEachUnitOfWork<TTevent>(ITeventObservationRegistrar this, Func<IReadOnlyList<TTevent>, Task> observer)`
- `TeventObservationRegistrar`
  - method: `public TeventObservationRegistrar AtRuntime<TTevent>()`
  - method: `public TeventObservationRegistrar ForEachUnitOfWork<TTevent>(Action<IReadOnlyList<TTevent>, IScopeResolver> observer)`
  - method: `public TeventObservationRegistrar ForEachUnitOfWork<TTevent>(Func<IReadOnlyList<TTevent>, IScopeResolver, Task> observer)`
- `TeventObservationRegistrarCE`
  - method: `public static TeventObservationRegistrar ForEachUnitOfWork<TTevent, TDependency1, TDependency2>(TeventObservationRegistrar this, Action<IReadOnlyList<TTevent>, TDependency1, TDependency2> observer)`
  - method: `public static TeventObservationRegistrar ForEachUnitOfWork<TTevent, TDependency1, TDependency2>(TeventObservationRegistrar this, Func<IReadOnlyList<TTevent>, TDependency1, TDependency2, Task> observer)`
  - method: `public static TeventObservationRegistrar ForEachUnitOfWork<TTevent, TDependency1>(TeventObservationRegistrar this, Action<IReadOnlyList<TTevent>, TDependency1> observer)`
  - method: `public static TeventObservationRegistrar ForEachUnitOfWork<TTevent, TDependency1>(TeventObservationRegistrar this, Func<IReadOnlyList<TTevent>, TDependency1, Task> observer)`
  - method: `public static TeventObservationRegistrar ForEachUnitOfWork<TTevent>(TeventObservationRegistrar this, Action<IReadOnlyList<TTevent>> observer)`
  - method: `public static TeventObservationRegistrar ForEachUnitOfWork<TTevent>(TeventObservationRegistrar this, Func<IReadOnlyList<TTevent>, Task> observer)`

### Compze.Tessaging - Removed members

(none)

### Compze.Tessaging - Changed members

(none)

## Compze.Teventive.TeventStore

Old: `C:\Dev\Compze.worktrees\api-base-e29fd66\src\Compze.Teventive.TeventStore\bin\Debug\net11.0\Compze.Teventive.TeventStore.dll` ; New: `C:\Dev\Compze\src\Compze.Teventive.TeventStore\bin\Debug\net11.0\Compze.Teventive.TeventStore.dll`

Counts reported by the tool: types added 2, types removed 12, types changed 5, members added 12, members removed 14, members changed 2, breaking 28.

### Compze.Teventive.TeventStore - Added types

**Namespace `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels`**

- `public interface Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>.ICollectionManager`
  - `IReadOnlyEntityCollection<TEntity, TEntityId> Entities { get; }`

**Namespace `Compze.Teventive.TeventStore.Wiring`**

- `public sealed class Compze.Teventive.TeventStore.Wiring.TeventStoreBuilder`
  - `public TeventStoreBuilder Taggregate<TTaggregate>()`

### Compze.Teventive.TeventStore - Removed types

**Namespace `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels`**

- `public interface Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.IQueryModelEntityCollectionManager<TEntity, TEntityId>` (breaking)
- `public interface Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.IReadonlyQueryModelEntityCollection<TEntity, TEntityId>` (breaking)
- `public class Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.QueryModelEntityCollection<TEntity, TEntityId>` (breaking)
- `public abstract class Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.NestedComponent<TNestedComponent, TNestedComponentTevent>` (breaking)
- `public abstract class Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.NestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>` (breaking)
- `public class Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.NestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>.CollectionManager` (breaking)
- `public class Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.QueryModelEntityCollectionManager<TParent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>` (breaking)
- `public class Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.QueryModelEntityCollectionManager<TParent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>` (breaking)
- `public abstract class Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.RemovableNestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>` (breaking)
- `public interface Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.RemovableNestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>.ICollectionManager` (breaking)
- `public class Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>.CollectionManager` (breaking)
- `public class Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>.CollectionManager` (breaking)

### Compze.Teventive.TeventStore - Changed types

**Namespace `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels`**

- `public abstract class Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>`
  - now derives from `Compze.Teventive.Taggregates.BaseClasses.TeventApplyingRoot`2<!0,!1>`
  - now implements `Compze.Teventive.ITeventRouted`
- `public abstract class Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>`
  - now derives from `Compze.Teventive.Taggregates.BaseClasses.TeventApplyingComponent`2<!2,!3>`
  - now implements `Compze.Teventive.ITeventRouted`
  - now implements `Compze.Teventive.ITeventRoutedMember`1<!3>`
- `public abstract class Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>`
  - now derives from `Compze.Teventive.Taggregates.BaseClasses.TeventApplyingComponent`2<!2,!4>`
  - now implements `Compze.Teventive.ITeventRouted`
  - now implements `Compze.Teventive.ITeventRoutedMember`1<!4>`
- `public abstract class Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>`
  - now derives from `Compze.Teventive.Taggregates.BaseClasses.TeventApplyingComponent`2<!2,!4>`
  - now implements `Compze.Teventive.ITeventRouted`
  - now implements `Compze.Teventive.ITeventRoutedMember`1<!4>`

**Namespace `Compze.Teventive.TeventStore.Refactoring.Migrations`**

- `public abstract class Compze.Teventive.TeventStore.Refactoring.Migrations.TeventMigration<TMigratedTaggregateTeventHierarchyRootInterface>` (breaking)
  - generic constraint added: TMigratedTaggregateTeventHierarchyRootInterface : Compze.Teventive.Taggregates.Tevents.ITaggregateTeventWithIdentity (breaking)
  - generic constraint removed: TMigratedTaggregateTeventHierarchyRootInterface : Compze.Teventive.Taggregates.Tevents.ITaggregateTevent

### Compze.Teventive.TeventStore - Added members

**Namespace `Compze.Teventive.TeventStore`**

- `TeventStore`
  - method: `public sealed void SaveSingleTaggregateTevents(IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> wrappedTevents)`
  - method: `public sealed void StreamTevents(int batchSize, Action<IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>>> handleTevents)`

**Namespace `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels`**

- `SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>`
  - constructor: `` protected SelfGeneratingQueryModel`2() ``
- `SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>`
  - constructor: `` protected Component`2(ITeventRouted parent) ``
- `SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>`
  - constructor: `` protected Entity`6() ``
  - method: `public static ICollectionManager<TQueryModel, TTaggregateTevent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter> CreateSelfManagingCollection(ITeventRouted parent)`
  - method: `protected static TeventRouting<TEntity, TEntityTevent> DeclareAppliers(Action<Declaration<TEntity, TEntityTevent>> declare)`
- `SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>`
  - constructor: `` protected Entity`5() ``
  - method: `public static ICollectionManager<TQueryModel, TTaggregateTevent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter> CreateSelfManagingCollection(ITeventRouted parent)`
  - method: `protected static TeventRouting<TEntity, TEntityTevent> DeclareAppliers(Action<Declaration<TEntity, TEntityTevent>> declare)`

**Namespace `Compze.Teventive.TeventStore.Wiring`**

- `TeventStoreRegistrar`
  - method: `public static IComponentRegistrar TeventStore(IComponentRegistrar registrar, string connectionName, Action<TeventStoreBuilder> declare = null)`
  - method: `public static IComponentRegistrar TeventStoreForFlexibleTesting(IComponentRegistrar registrar, string connectionName, Func<IReadOnlyList<ITeventMigration>> migrations, Action<TeventStoreBuilder> declare = null)`

### Compze.Teventive.TeventStore - Removed members

**Namespace `Compze.Teventive.TeventStore`**

- `TeventStore`
  - method: `public sealed void SaveSingleTaggregateTevents(IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> wrappedTevents)` (breaking) - member removed or no longer visible
  - method: `public sealed void StreamTevents(int batchSize, Action<IReadOnlyList<ITaggregateTevent<ITaggregateTevent>>> handleTevents)` (breaking) - member removed or no longer visible

**Namespace `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels`**

- `SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>`
  - constructor: `` protected SelfGeneratingQueryModel`2(TeventDispatcherConfig teventAppliersDispatcherConfig = null) `` (breaking) - member removed or no longer visible
  - method: `protected virtual void AssertInvariantsAreMet()` - no longer declared here but still inherited from a base type; callers are unaffected
  - method: `protected ITeventSubscriber<TTaggregateTevent> RegisterTeventAppliers()` (breaking) - member removed or no longer visible
- `SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>`
  - constructor: `` protected Component`2(ITeventSubscriber<TComponentTevent> appliersSubscriber, bool registerTeventAppliers, TeventDispatcherConfig teventAppliersDispatcherConfig = null) `` (breaking) - member removed or no longer visible
  - constructor: `` protected Component`2(TQueryModel queryModel) `` (breaking) - member removed or no longer visible
  - method: `protected ITeventSubscriber<TComponentTevent> RegisterTeventAppliers()` (breaking) - member removed or no longer visible
- `SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>`
  - constructor: `` protected Entity`6(TQueryModel queryModel) `` (breaking) - member removed or no longer visible
  - method: `public static CollectionManager<TQueryModel, TTaggregateTevent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter> CreateSelfManagingCollection(TQueryModel parent)` (breaking) - member removed or no longer visible
- `SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>`
  - constructor: `` protected Entity`5(TQueryModel queryModel, TeventDispatcherConfig teventAppliersDispatcherConfig = null) `` (breaking) - member removed or no longer visible
  - method: `public static CollectionManager<TQueryModel, TTaggregateTevent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter> CreateSelfManagingCollection(TQueryModel parent)` (breaking) - member removed or no longer visible

**Namespace `Compze.Teventive.TeventStore.Wiring`**

- `TeventStoreRegistrar`
  - method: `public static IComponentRegistrar TeventStore(IComponentRegistrar registrar, string connectionName)` (breaking) - member removed or no longer visible
  - method: `public static IComponentRegistrar TeventStoreForFlexibleTesting(IComponentRegistrar registrar, string connectionName, Func<IReadOnlyList<ITeventMigration>> migrations)` (breaking) - member removed or no longer visible

### Compze.Teventive.TeventStore - Changed members

**Namespace `Compze.Teventive.TeventStore`**

- `TeventStore`
  - method `GetTaggregateHistory` (breaking)
    - old: `public sealed IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetTaggregateHistory(TaggregateId id)`
    - new: `public sealed IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> GetTaggregateHistory(TaggregateId id)`
    - reason: return type changed from IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> to IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>>
  - method `GetTaggregateHistoryForUpdate` (breaking)
    - old: `public sealed IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetTaggregateHistoryForUpdate(TaggregateId id)`
    - new: `public sealed IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> GetTaggregateHistoryForUpdate(TaggregateId id)`
    - reason: return type changed from IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> to IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>>

## Compze.Teventive.TeventStore.Abstractions

Old: `C:\Dev\Compze.worktrees\api-base-e29fd66\src\Compze.Teventive.TeventStore.Abstractions\bin\Debug\net11.0\Compze.Teventive.TeventStore.Abstractions.dll` ; New: `C:\Dev\Compze\src\Compze.Teventive.TeventStore.Abstractions\bin\Debug\net11.0\Compze.Teventive.TeventStore.Abstractions.dll`

Counts reported by the tool: types added 0, types removed 1, types changed 1, members added 5, members removed 6, members changed 4, breaking 16.

### Compze.Teventive.TeventStore.Abstractions - Added types

(none)

### Compze.Teventive.TeventStore.Abstractions - Removed types

**Namespace `Compze.Teventive.TeventStore.Abstractions`**

- `public interface Compze.Teventive.TeventStore.Abstractions.ITeventStoreUpdater` (breaking)

### Compze.Teventive.TeventStore.Abstractions - Changed types

**Namespace `Compze.Teventive.TeventStore.Abstractions`**

- `public interface Compze.Teventive.TeventStore.Abstractions.ITeventStoreReader`
  - now implements Compze.Teventive.TaggregateStores.ITaggregateStoreReader

### Compze.Teventive.TeventStore.Abstractions - Added members

**Namespace `Compze.Teventive.TeventStore.Abstractions`**

- `ITeventStore`
  - method: `void SaveSingleTaggregateTevents(IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> wrappedTevents)` (breaking) - abstract member added to an interface; implementers must add it
  - method: `void StreamTevents(int batchSize, Action<IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>>> handleTevents)` (breaking) - abstract member added to an interface; implementers must add it

**Namespace `Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations`**

- `ISingleTaggregateInstanceHandlingTeventMigrator`
  - method: `void MigrateTevent(ITaggregateTevent<ITaggregateTeventWithIdentity> wrappedTevent, ITeventModifier modifier)` (breaking) - abstract member added to an interface; implementers must add it
- `ITeventModifier`
  - method: `void InsertBefore(params ITaggregateTevent<ITaggregateTeventWithIdentity>[] insert)` (breaking) - abstract member added to an interface; implementers must add it
  - method: `void Replace(params ITaggregateTevent<ITaggregateTeventWithIdentity>[] wrappedTevents)` (breaking) - abstract member added to an interface; implementers must add it

### Compze.Teventive.TeventStore.Abstractions - Removed members

**Namespace `Compze.Teventive.TeventStore.Abstractions`**

- `ITeventStore`
  - method: `void SaveSingleTaggregateTevents(IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> wrappedTevents)` (breaking) - member removed or no longer visible
  - method: `void StreamTevents(int batchSize, Action<IReadOnlyList<ITaggregateTevent<ITaggregateTevent>>> handleTevents)` (breaking) - member removed or no longer visible
- `ITeventStoreReader`
  - method: `TTaggregate GetReadonlyCopy<TTaggregate>(TaggregateId taggregateId)` (breaking) - member removed or no longer visible

**Namespace `Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations`**

- `ISingleTaggregateInstanceHandlingTeventMigrator`
  - method: `void MigrateTevent(ITaggregateTevent<ITaggregateTevent> wrappedTevent, ITeventModifier modifier)` (breaking) - member removed or no longer visible
- `ITeventModifier`
  - method: `void InsertBefore(params ITaggregateTevent<ITaggregateTevent>[] insert)` (breaking) - member removed or no longer visible
  - method: `void Replace(params ITaggregateTevent<ITaggregateTevent>[] wrappedTevents)` (breaking) - member removed or no longer visible

### Compze.Teventive.TeventStore.Abstractions - Changed members

**Namespace `Compze.Teventive.TeventStore.Abstractions`**

- `ITeventStore`
  - method `GetTaggregateHistory` (breaking)
    - old: `IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetTaggregateHistory(TaggregateId id)`
    - new: `IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> GetTaggregateHistory(TaggregateId id)`
    - reason: return type changed from IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> to IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>>
  - method `GetTaggregateHistoryForUpdate` (breaking)
    - old: `IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetTaggregateHistoryForUpdate(TaggregateId id)`
    - new: `IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> GetTaggregateHistoryForUpdate(TaggregateId id)`
    - reason: return type changed from IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> to IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>>
- `ITeventStoreReader`
  - method `GetHistory` (breaking)
    - old: `IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetHistory(TaggregateId taggregateId)`
    - new: `IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> GetHistory(TaggregateId taggregateId)`
    - reason: return type changed from IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> to IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>>
- `TeventStoreTestingExtensions`
  - method `ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize` (breaking)
    - old: `public static IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize(ITeventStore this, int batchSize = 10000)`
    - new: `public static IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize(ITeventStore this, int batchSize = 10000)`
    - reason: return type changed from IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> to IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>>

## Compze.Teventive.TeventStore.Typermedia

Old: `C:\Dev\Compze.worktrees\api-base-e29fd66\src\Compze.Teventive.TeventStore.Typermedia\bin\Debug\net11.0\Compze.Teventive.TeventStore.Typermedia.dll` ; New: `C:\Dev\Compze\src\Compze.Teventive.TeventStore.Typermedia\bin\Debug\net11.0\Compze.Teventive.TeventStore.Typermedia.dll`

Counts reported by the tool: types added 0, types removed 0, types changed 5, members added 0, members removed 0, members changed 0, breaking 5.

### Compze.Teventive.TeventStore.Typermedia - Added types

(none)

### Compze.Teventive.TeventStore.Typermedia - Removed types

(none)

### Compze.Teventive.TeventStore.Typermedia - Changed types

**Namespace `Compze.Teventive.TeventStore.Typermedia`**

- `public class Compze.Teventive.TeventStore.Typermedia.TeventStoreApi.TommandApi.SaveTaggregate<TTaggregate>` (breaking)
  - generic constraint added: TTaggregate : Compze.Teventive.ITaggregate<Compze.Teventive.Taggregates.Tevents.ITaggregateTeventWithIdentity> (breaking)
  - generic constraint removed: TTaggregate : Compze.Teventive.ITaggregate
- `public class Compze.Teventive.TeventStore.Typermedia.TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregate<TTaggregate>` (breaking)
  - generic constraint added: TTaggregate : Compze.Teventive.ITaggregate<Compze.Teventive.Taggregates.Tevents.ITaggregateTeventWithIdentity> (breaking)
  - generic constraint removed: TTaggregate : Compze.Teventive.ITaggregate
- `public class Compze.Teventive.TeventStore.Typermedia.TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregateVersion<TTaggregate>` (breaking)
  - generic constraint added: TTaggregate : Compze.Teventive.ITaggregate<Compze.Teventive.Taggregates.Tevents.ITaggregateTeventWithIdentity> (breaking)
  - generic constraint removed: TTaggregate : Compze.Teventive.ITaggregate
- `public class Compze.Teventive.TeventStore.Typermedia.TeventStoreApi.TueryApi.GetTaggregateHistory<TTevent>` (breaking)
  - generic constraint added: TTevent : class (breaking)
  - generic constraint added: TTevent : Compze.Teventive.Taggregates.Tevents.ITaggregateTeventWithIdentity (breaking)
  - generic constraint removed: TTevent : Compze.Teventive.Taggregates.Tevents.ITaggregateTevent
- `public class Compze.Teventive.TeventStore.Typermedia.TeventStoreApi.TueryApi.TaggregateLink<TTaggregate>` (breaking)
  - generic constraint added: TTaggregate : Compze.Teventive.ITaggregate<Compze.Teventive.Taggregates.Tevents.ITaggregateTeventWithIdentity> (breaking)
  - generic constraint removed: TTaggregate : Compze.Teventive.ITaggregate

### Compze.Teventive.TeventStore.Typermedia - Added members

(none)

### Compze.Teventive.TeventStore.Typermedia - Removed members

(none)

### Compze.Teventive.TeventStore.Typermedia - Changed members

(none)

## Compze.Teventive.StateStore (new assembly, no old side)

New: `C:\Dev\Compze\src\Compze.Teventive.StateStore\bin\Debug\net11.0\Compze.Teventive.StateStore.dll` (assembly version 0.1.0.0). Listed with sherlock `get_types_from_assembly` and `get_type_members`; the assembly loaded without a missing-dependency problem. Compiler-generated non-public nested types (closure classes) are not listed. Variance and constraints are as reported by sherlock's full type listing.

### Compze.Teventive.StateStore - Public types

**Namespace `Compze.Teventive.StateStore`**

- `public interface Compze.Teventive.StateStore.IStateStore`
  - `ReloadedTaggregates ReloadFromStorage()`
  - `ReloadedTaggregates ReloadFromStorageAfter(Action changeToTheStorage)`
- `public sealed class Compze.Teventive.StateStore.ReloadedTaggregates`
  - `public IReadOnlyList<TaggregateId> Added { get; }`
  - `public IReadOnlyList<TaggregateId> Changed { get; }`
  - `public IReadOnlyList<TaggregateId> Removed { get; }`
- `public interface Compze.Teventive.StateStore.ITaggregateFilesInMemory`
  - `IReadOnlyCollection<string> Paths { get; }`
  - `void Delete(string path)`
  - `string ReadAllText(string path)`
  - `void WriteAllText(string path, string content)`
- `public interface Compze.Teventive.StateStore.ITaggregateIndex<out TTaggregate, in TKey> where TTaggregate : class, Compze.Teventive.ITaggregate` (variance and constraint from sherlock's flags: TTaggregate covariant and reference-type constrained, TKey contravariant)
  - `bool HasAny(TKey key)`
  - `IReadOnlyCollection<ITaggregateReference<TTaggregate>> Lookup(TKey key)`
- `public sealed class Compze.Teventive.StateStore.StateStoreBuilder`
  - `public StateStoreBuilder Index<TTaggregate, TKey>(Func<TTaggregate, IEnumerable<TKey>> keysOf)`
  - `public StateStoreBuilder StoredInFiles(DirectoryInfo folder, JsonSerializerOptions options, Action<TaggregateFiles> declare)`
  - `public StateStoreBuilder StoredInFiles<TService>(Func<TService, DirectoryInfo> folderWith, JsonSerializerOptions options, Action<TaggregateFiles> declare)`
  - `public StateStoreBuilder Taggregate<TTaggregate, TSnapshot>(Func<ISnapshotable<TTaggregate, TSnapshot>> createEmpty, Func<TSnapshot, TSnapshot, bool> snapshotsAreEqual = null)`
  - `public StateStoreBuilder Taggregate<TTaggregate, TSnapshot, TService>(Func<TService, ISnapshotable<TTaggregate, TSnapshot>> createEmpty, Func<TSnapshot, TSnapshot, bool> snapshotsAreEqual = null)`
- `public static class Compze.Teventive.StateStore.StateStoreRegistrar`
  - `public static IComponentRegistrar StateStore(IComponentRegistrar this, Action<StateStoreBuilder> declare)`
- `public sealed class Compze.Teventive.StateStore.TaggregateFiles`
  - `public TaggregateFiles KeptInMemory()`
  - `public TaggregateFiles Of<TTaggregate>(string folder, Func<TTaggregate, DirectoryInfo, FileInfo> fileOf = null)`

## Compze.Teventive.StateStore.MemoryPack (new assembly, no old side)

New: `C:\Dev\Compze\src\Compze.Teventive.StateStore.MemoryPack\bin\Debug\net11.0\Compze.Teventive.StateStore.MemoryPack.dll` (assembly version 0.1.0.0). Listed with sherlock `get_types_from_assembly` and `get_type_members`; the assembly loaded without a missing-dependency problem. The two nested public types below are compiler-generated containers for the C# extension block, reported by sherlock as public nested types; they are listed because sherlock lists them.

### Compze.Teventive.StateStore.MemoryPack - Public types

**Namespace `Compze.Teventive.StateStore.MemoryPack`**

- `public static class Compze.Teventive.StateStore.MemoryPack.TaggregateFilesMemoryPackCE`
  - `public static TaggregateFiles CachedWithMemoryPack(TaggregateFiles this)`
- ``public sealed class Compze.Teventive.StateStore.MemoryPack.TaggregateFilesMemoryPackCE.<G>$2D77095F2C4A4556312F9B8B25B7F34F`` (compiler-generated extension-block container)
  - `public TaggregateFiles CachedWithMemoryPack()`
- ``public static class Compze.Teventive.StateStore.MemoryPack.TaggregateFilesMemoryPackCE.<G>$2D77095F2C4A4556312F9B8B25B7F34F.<M>$F26B29F148190FE7E763051717D7D86D`` (compiler-generated extension-block marker)
  - `public static void <Extension>$(TaggregateFiles this)`
