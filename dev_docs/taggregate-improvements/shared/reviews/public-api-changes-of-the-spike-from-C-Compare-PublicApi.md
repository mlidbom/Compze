# Public API changes: taggregate-store-spike (46cc881a6), against its merge-base with e29fd667327ff451e0ea74297992df1d205eb58d (e29fd6673)

Made by C-Compare-PublicApi, from the declarations DocFX's metadata step gives each public and protected type and member. A method whose parameter types changed is listed as one removed and one added.

**Not in this report: the members of C# 14 extension blocks.** DocFX skips them (its pull request 10943 made it skip them rather than fail), so compare them with git diff. These changed files hold extension blocks:

- `src/Compze.DependencyInjection/IScopeFactoryUnitOfWorkRunner.cs`
- `src/Compze.Tessaging/TessageBus/ITeventObservationRegistrarCE.cs`
- `src/Compze.Tessaging/TessageBus/TeventObservationRegistrarCE.cs`
- `src/Compze.Teventive.StateStore.MemoryPack/TaggregateFilesMemoryPackCE.cs`

## Compze.Tessaging

1 types added, 0 removed, 0 changed; in the types on both sides, 7 members added, 0 removed, 0 changed.

### Added types

- `Compze.Tessaging.TessageBus.ITeventObservation`: `public interface ITeventObservation`
  - `IDisposable ObserveTevents(Action<TeventObservationRegistrar> observe)`

### Changed types

#### `Compze.Tessaging.Engine.ILocalTessagingEngine`

- added: `Task AwaitNoTessagesInFlightAsync()`

#### `Compze.Tessaging.TessageBus.ITeventObservationRegistrar`

- added: `ITeventObservationRegistrar AtRuntime<TTevent>() where TTevent : ITevent`
- added: `ITeventObservationRegistrar ForEachUnitOfWork<TTevent>(Action<IReadOnlyList<TTevent>, IScopeResolver> observer) where TTevent : ITevent`
- added: `ITeventObservationRegistrar ForEachUnitOfWork<TTevent>(Func<IReadOnlyList<TTevent>, IScopeResolver, Task> observer) where TTevent : ITevent`

#### `Compze.Tessaging.TessageBus.TeventObservationRegistrar`

- added: `public TeventObservationRegistrar AtRuntime<TTevent>() where TTevent : ITevent`
- added: `public TeventObservationRegistrar ForEachUnitOfWork<TTevent>(Action<IReadOnlyList<TTevent>, IScopeResolver> observer) where TTevent : ITevent`
- added: `public TeventObservationRegistrar ForEachUnitOfWork<TTevent>(Func<IReadOnlyList<TTevent>, IScopeResolver, Task> observer) where TTevent : ITevent`

## Compze.Teventive

18 types added, 2 removed, 20 changed; in the types on both sides, 16 members added, 16 removed, 0 changed.

### Added types

- `Compze.Teventive.IMutableTaggregateTeventWithIdentity`: `public interface IMutableTaggregateTeventWithIdentity : IMutableTaggregateTevent, ITaggregateTeventWithIdentity, ITaggregateTevent, ITevent, ITessageWithIdentity, ITessage`
  - `[Obsolete("\r\nThis member breaks encapsulation and framework guarantees if used incorrectly.\r\nIt is for building infrastructure code, usually within the framework itself.\r\nBe VERY sure you know what you are doing before you use this member.\r\n")] void SetTessageIdInternal(TessageId tessageId)`

- `Compze.Teventive.ITeventRouted`: `public interface ITeventRouted`
  - `TeventRouting? TeventAppliers { get; }`
  - `TeventRouting? TeventHandlers { get; }`

- `Compze.Teventive.ITeventRoutedMember<TTevent>`: `public interface ITeventRoutedMember<TTevent> where TTevent : ITevent`
  - `[Obsolete("\r\nThis member breaks encapsulation and framework guarantees if used incorrectly.\r\nIt is for building infrastructure code, usually within the framework itself.\r\nBe VERY sure you know what you are doing before you use this member.\r\n")] void DispatchInternal(IPublisherTevent<ITevent> wrapped)`

- `Compze.Teventive.Taggregates.BaseClasses.Shared.SharedTomponentSlotInAnOwner<TTomponentTevent>`: `public readonly ref struct SharedTomponentSlotInAnOwner<TTomponentTevent> where TTomponentTevent : class, ITevent`

- `Compze.Teventive.Taggregates.BaseClasses.Shared.SharedTomponentSlotOfAClass<TOwner, TOwnerTevent, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent>`: `public sealed class SharedTomponentSlotOfAClass<TOwner, TOwnerTevent, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent> where TOwner : class where TOwnerTevent : class, ITaggregateTevent where TOwnerTeventImplementation : TaggregateTevent, TOwnerTevent where TTomponentTevent : class, ITevent where TAdoptingWrapperTevent : class, TOwnerTevent, IPublisherTevent<TTomponentTevent>`
  - `public SharedTomponentSlotInAnOwner<TTomponentTevent> In(TOwner owner)`
  - `public SharedTomponentSlotOfAClass(Type adoptingWrapperTeventImplementation, Func<TOwner, ISharedTomponentInternals<TTomponentTevent>> tomponentOf)`

- `Compze.Teventive.Taggregates.BaseClasses.TeventApplyingComponent<TComponent, TComponentTevent>`: `public abstract class TeventApplyingComponent<TComponent, TComponentTevent> : ITeventRoutedMember<TComponentTevent>, ITeventRouted where TComponent : TeventApplyingComponent<TComponent, TComponentTevent> where TComponentTevent : class, ITevent`
  - `protected static TeventRouting<TComponent, TComponentTevent> DeclareAppliers(Action<TeventRouting<TComponent, TComponentTevent>.Declaration> declare)`

- `Compze.Teventive.Taggregates.BaseClasses.TeventApplyingRoot<TRoot, TTevent>`: `public abstract class TeventApplyingRoot<TRoot, TTevent> : VersionedEntity<TRoot>, IEntity, IEntity<Guid>, ITeventRouted where TRoot : TeventApplyingRoot<TRoot, TTevent> where TTevent : class, ITaggregateTevent`
  - `protected static TeventRouting<TRoot, TTevent> DeclareAppliers(Action<TeventRouting<TRoot, TTevent>.Declaration> declare)`
  - `protected virtual void AssertInvariantsAreMet()`

- `Compze.Teventive.Taggregates.ISnapshotable<TSelf, TSnapshot>`: `public interface ISnapshotable<TSelf, TSnapshot> where TSelf : ISnapshotable<TSelf, TSnapshot>`
  - `TSnapshot TakeSnapshot()`
  - `void RestoreFrom(TSnapshot snapshot)`

- `Compze.Teventive.Taggregates.SnapshotRoundTrip`: `public static class SnapshotRoundTrip`
  - `public static IReadOnlyList<string> DifferencesOf<TTaggregate, TSnapshot>(TTaggregate taggregate, Func<ISnapshotable<TTaggregate, TSnapshot>> createEmpty, Func<TSnapshot, TSnapshot, bool>? snapshotsAreEqual = null) where TTaggregate : class, ITaggregate, ISnapshotable<TTaggregate, TSnapshot>`
  - `public static void AssertLosesNothing<TTaggregate, TSnapshot>(TTaggregate taggregate, Func<ISnapshotable<TTaggregate, TSnapshot>> createEmpty, Func<TSnapshot, TSnapshot, bool>? snapshotsAreEqual = null) where TTaggregate : class, ITaggregate, ISnapshotable<TTaggregate, TSnapshot>`

- `Compze.Teventive.Taggregates.Tevents.ITaggregateTeventWithIdentity`: `public interface ITaggregateTeventWithIdentity : ITaggregateTevent, ITevent, ITessageWithIdentity, ITessage`

- `Compze.Teventive.Taggregates.Tevents.TaggregateTeventWithIdentity`: `public abstract class TaggregateTeventWithIdentity : TaggregateTevent, IMutableTaggregateTeventWithIdentity, IMutableTaggregateTevent, ITaggregateTeventWithIdentity, ITaggregateTevent, ITevent, ITessageWithIdentity, ITessage`
  - `protected TaggregateTeventWithIdentity()`
  - `protected TaggregateTeventWithIdentity(TaggregateId taggregateId)`
  - `public TessageId Id { get; }`

- `Compze.Teventive.TaggregateStores.ITaggregateReference<TTaggregate>`: `public interface ITaggregateReference<out TTaggregate> where TTaggregate : class, ITaggregate`
  - `TaggregateId Id { get; }`
  - `TTaggregate Current { get; }`
  - `TTaggregate ForUpdate()`

- `Compze.Teventive.TaggregateStores.ITaggregateStoreBulkReader`: `public interface ITaggregateStoreBulkReader : ITaggregateStoreReader`
  - `IReadOnlyCollection<ITaggregateReference<TTaggregate>> All<TTaggregate>() where TTaggregate : class, ITaggregate`

- `Compze.Teventive.TaggregateStores.ITaggregateStoreReader`: `public interface ITaggregateStoreReader`
  - `bool TryGet<TTaggregate>(TaggregateId id, out TTaggregate? taggregate) where TTaggregate : class, ITaggregate`
  - `ITaggregateReference<TTaggregate> Reference<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate`
  - `TTaggregate Get<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate`

- `Compze.Teventive.TaggregateStores.ITaggregateStoreUpdater`: `public interface ITaggregateStoreUpdater : ITaggregateStoreReader`
  - `bool TryGetForUpdate<TTaggregate>(TaggregateId id, out TTaggregate? taggregate) where TTaggregate : class, ITaggregate`
  - `TTaggregate GetForUpdate<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate`
  - `void Save<TTaggregate>(TTaggregate taggregate) where TTaggregate : class, ITaggregate`

- `Compze.Teventive.TeventRouting`: `public abstract class TeventRouting`
  - `protected TeventRouting() (implicit)`

- `Compze.Teventive.TeventRouting<TInstance, TTevent>`: `public sealed class TeventRouting<TInstance, TTevent> : TeventRouting where TInstance : class where TTevent : ITevent`
  - `public TeventRouting<TSubclass, TTevent> Extend<TSubclass>(Action<TeventRouting<TSubclass, TTevent>.Declaration> declare) where TSubclass : class, TInstance`

- `Compze.Teventive.TeventRouting<TInstance, TTevent>.Declaration`: `public sealed class TeventRouting<TInstance, TTevent>.Declaration`
  - `public TeventRouting<TInstance, TTevent>.Declaration Component<TComponentTevent>(Func<TInstance, ITeventRoutedMember<TComponentTevent>> component) where TComponentTevent : TTevent`
  - `public TeventRouting<TInstance, TTevent>.Declaration Entities<TEntityTevent>(Func<TInstance, ITeventRoutedMember<TEntityTevent>> entities) where TEntityTevent : TTevent`
  - `public TeventRouting<TInstance, TTevent>.Declaration For<THandledTevent>(Action<TInstance, THandledTevent> handler) where THandledTevent : TTevent`
  - `public TeventRouting<TInstance, TTevent>.Declaration IgnoreUnhandled<TIgnoredTevent>() where TIgnoredTevent : TTevent`
  - `public TeventRouting<TInstance, TTevent>.Declaration SharedTomponent<TOwnerTevent, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent>(SharedTomponentSlotOfAClass<TInstance, TOwnerTevent, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent> slot) where TOwnerTevent : class, ITaggregateTevent where TOwnerTeventImplementation : TaggregateTevent, TOwnerTevent where TTomponentTevent : class, ITevent where TAdoptingWrapperTevent : class, TOwnerTevent, IPublisherTevent<TTomponentTevent>, TTevent`

### Removed types

- `Compze.Teventive.Taggregates.BaseClasses.Shared.ISharedTomponentSlot<TTomponentTevent>`: `public interface ISharedTomponentSlot<TTomponentTevent> where TTomponentTevent : class, ITevent`
- `Compze.Teventive.Taggregates.BaseClasses.Shared.SharedTomponentSlot<TOwnerTevent, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent>`: `public sealed class SharedTomponentSlot<TOwnerTevent, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent> : ISharedTomponentSlot<TTomponentTevent> where TOwnerTevent : class, ITaggregateTevent where TOwnerTeventImplementation : TaggregateTevent, TOwnerTevent where TTomponentTevent : class, ITevent where TAdoptingWrapperTevent : class, TOwnerTevent, IPublisherTevent<TTomponentTevent>`

### Changed types

#### `Compze.Teventive.IMutableTaggregateTevent`

- changed: `public interface IMutableTaggregateTevent : ITaggregateTevent, IExactlyOnceTevent, IRemotableTevent, ITevent, IExactlyOnceTessage, IMustBeSentAndHandledTransactionally, IMustBeSentTransactionally, IAtMostOnceTessage, IMustBeHandledTransactionally, IAtLeastOnceTessage, ITessageWithIdentity, IRemotableTessage, ITessage`
  to: `public interface IMutableTaggregateTevent : ITaggregateTevent, ITevent, ITessage`
- removed: `[Obsolete("\r\nThis member breaks encapsulation and framework guarantees if used incorrectly.\r\nIt is for building infrastructure code, usually within the framework itself.\r\nBe VERY sure you know what you are doing before you use this member.\r\n")] void SetTessageIdInternal(TessageId tessageId)`

#### `Compze.Teventive.Taggregates.BaseClasses.IEntityCollectionManager<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent>`

- added: `TEntity AddRestored(TEntityId id)`

#### `Compze.Teventive.Taggregates.BaseClasses.ITeventiveInternals<TTevent, TTeventImplementation>`

- changed: `public interface ITeventiveInternals<in TTevent, in TTeventImplementation> where TTevent : class, ITaggregateTevent where TTeventImplementation : TaggregateTevent, TTevent`
  to: `public interface ITeventiveInternals<in TTevent, in TTeventImplementation> : ITeventRouted where TTevent : class, ITaggregateTevent where TTeventImplementation : TaggregateTevent, TTevent`
- removed: `[Obsolete("\r\nThis member breaks encapsulation and framework guarantees if used incorrectly.\r\nIt is for building infrastructure code, usually within the framework itself.\r\nBe VERY sure you know what you are doing before you use this member.\r\n")] ITeventSubscriber<in TTevent> RegisterTeventAppliersInternal()`
- removed: `[Obsolete("\r\nThis member breaks encapsulation and framework guarantees if used incorrectly.\r\nIt is for building infrastructure code, usually within the framework itself.\r\nBe VERY sure you know what you are doing before you use this member.\r\n")] void ApplyTeventInternal(TTevent tevent)`

#### `Compze.Teventive.Taggregates.BaseClasses.Shared.ISharedTomponentInternals<TTomponentTevent>`

- removed: `[Obsolete("\r\nThis member breaks encapsulation and framework guarantees if used incorrectly.\r\nIt is for building infrastructure code, usually within the framework itself.\r\nBe VERY sure you know what you are doing before you use this member.\r\n")] void ApplyTeventInternal(TTomponentTevent tevent)`
- added: `[Obsolete("\r\nThis member breaks encapsulation and framework guarantees if used incorrectly.\r\nIt is for building infrastructure code, usually within the framework itself.\r\nBe VERY sure you know what you are doing before you use this member.\r\n")] void ApplyAdoptedTeventInternal(IPublisherTevent<TTomponentTevent> adopted)`

#### `Compze.Teventive.Taggregates.BaseClasses.Shared.SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>`

- changed: `public abstract class SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent> where TTentity : SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent> where TTentityId : struct where TTentityTevent : class, ISharedTentityTevent<TTentityId> where TTentityCreatedTevent : class, TTentityTevent`
  to: `public abstract class SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent> : ITeventRouted where TTentity : SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent> where TTentityId : struct where TTentityTevent : class, ISharedTentityTevent<TTentityId> where TTentityCreatedTevent : class, TTentityTevent`
- removed: `protected ITeventSubscriber<TTentityTevent> RegisterTeventAppliers()`
- added: `protected static TeventRouting<TTentity, TTentityTevent> DeclareAppliers(Action<TeventRouting<TTentity, TTentityTevent>.Declaration> declare)`

#### `Compze.Teventive.Taggregates.BaseClasses.Shared.SharedTentityCollection<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>`

- changed: `public class SharedTentityCollection<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent> : SharedTomponent<TTentityTevent>, ISharedTomponentInternals<TTentityTevent>, IEnumerable<TTentity>, IEnumerable where TTentity : SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent> where TTentityId : struct where TTentityTevent : class, ISharedTentityTevent<TTentityId> where TTentityCreatedTevent : class, TTentityTevent`
  to: `public sealed class SharedTentityCollection<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent> : SharedTomponent<TTentityTevent>, ISharedTomponentInternals<TTentityTevent>, ITeventRouted, IEnumerable<TTentity>, IEnumerable where TTentity : SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent> where TTentityId : struct where TTentityTevent : class, ISharedTentityTevent<TTentityId> where TTentityCreatedTevent : class, TTentityTevent`
- removed: `public SharedTentityCollection(ISharedTomponentSlot<TTentityTevent> slot)`
- added: `public SharedTentityCollection(SharedTomponentSlotInAnOwner<TTentityTevent> slot)`
- added: `public TTentity AddRestored(TTentityId id)`

#### `Compze.Teventive.Taggregates.BaseClasses.Shared.SharedTomponent<TTomponentTevent>`

- changed: `public abstract class SharedTomponent<TTomponentTevent> : ISharedTomponentInternals<TTomponentTevent> where TTomponentTevent : class, ITevent`
  to: `public abstract class SharedTomponent<TTomponentTevent> : ISharedTomponentInternals<TTomponentTevent>, ITeventRouted where TTomponentTevent : class, ITevent`
- removed: `protected ITeventSubscriber<TTomponentTevent> RegisterTeventAppliers()`
- removed: `protected SharedTomponent(ISharedTomponentSlot<TTomponentTevent> slot, TeventDispatcherConfig? teventAppliersDispatcherConfig = null)`
- added: `protected SharedTomponent(SharedTomponentSlotInAnOwner<TTomponentTevent> slot)`
- added: `protected static TeventRouting<TTomponent, TTomponentTevent> DeclareAppliers<TTomponent>(Action<TeventRouting<TTomponent, TTomponentTevent>.Declaration> declare) where TTomponent : SharedTomponent<TTomponentTevent>`

#### `Compze.Teventive.Taggregates.BaseClasses.Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>`

- changed: `public class Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation> : VersionedEntity<TTaggregate>, ITaggregate<TTaggregateTevent>, ITaggregate, ITentity, IEntity, IEntity<Guid>, ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation> where TTaggregate : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation> where TTaggregateTevent : class, ITaggregateTevent where TTaggregateTeventImplementation : TaggregateTevent, TTaggregateTevent where TWrapperTeventInterface : ITaggregateTevent<TTaggregateTevent> where TWrapperTeventImplementation : TWrapperTeventInterface`
  to: `public class Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation> : TeventApplyingRoot<TTaggregate, TTaggregateTevent>, ITaggregate<TTaggregateTevent>, ITaggregate, ITentity, IEntity, IEntity<Guid>, ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>, ITeventRouted where TTaggregate : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation> where TTaggregateTevent : class, ITaggregateTevent where TTaggregateTeventImplementation : TaggregateTevent, TTaggregateTevent where TWrapperTeventInterface : ITaggregateTevent<TTaggregateTevent> where TWrapperTeventImplementation : TWrapperTeventInterface`
- removed: `protected ITeventSubscriber<TTaggregateTevent> RegisterTeventAppliers()`
- removed: `protected ITeventSubscriber<TTaggregateTevent> RegisterTeventHandlers()`
- removed: `protected Taggregate(TeventDispatcherConfig? teventAppliersDispatcherConfig = null)`
- removed: `protected virtual void AssertInvariantsAreMet()`
- added: `protected static TeventRouting<TTaggregate, TTaggregateTevent> DeclareHandlers(Action<TeventRouting<TTaggregate, TTaggregateTevent>.Declaration> declare)`
- added: `protected void AssertCanBeChanged()`
- added: `protected void RestoreIdentity(TaggregateId id)`
- added: `public Taggregate() (implicit)`

#### `Compze.Teventive.Taggregates.BaseClasses.Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.Component<TComponent, TComponentTevent, TComponentTeventImplementation>`

- changed: `public abstract class Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.Component<TComponent, TComponentTevent, TComponentTeventImplementation> : TeventiveComponent<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>, ITeventiveInternals<TComponentTevent, TComponentTeventImplementation> where TComponent : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.Component<TComponent, TComponentTevent, TComponentTeventImplementation> where TComponentTevent : class, TTaggregateTevent where TComponentTeventImplementation : TTaggregateTeventImplementation, TComponentTevent`
  to: `public abstract class Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.Component<TComponent, TComponentTevent, TComponentTeventImplementation> : TeventiveComponent<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>, ITeventRoutedMember<TComponentTevent>, ITeventiveInternals<TComponentTevent, TComponentTeventImplementation>, ITeventRouted where TComponent : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.Component<TComponent, TComponentTevent, TComponentTeventImplementation> where TComponentTevent : class, TTaggregateTevent where TComponentTeventImplementation : TTaggregateTeventImplementation, TComponentTevent`

#### `Compze.Teventive.Taggregates.BaseClasses.Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.Entity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityTeventIdGetterSetter>`

- changed: `public abstract class Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.Entity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityTeventIdGetterSetter> : Tentity<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>, ITeventiveInternals<TEntityTevent, TEntityTeventImplementation> where TEntity : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.Entity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityTeventIdGetterSetter> where TEntityId : struct where TEntityTevent : class, TTaggregateTevent where TEntityTeventImplementation : TTaggregateTeventImplementation, TEntityTevent where TEntityCreatedTevent : TEntityTevent where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>`
  to: `public abstract class Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.Entity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityTeventIdGetterSetter> : Tentity<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>, ITeventRoutedMember<TEntityTevent>, ITeventiveInternals<TEntityTevent, TEntityTeventImplementation>, ITeventRouted where TEntity : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.Entity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityTeventIdGetterSetter> where TEntityId : struct where TEntityTevent : class, TTaggregateTevent where TEntityTeventImplementation : TTaggregateTeventImplementation, TEntityTevent where TEntityCreatedTevent : TEntityTevent where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>`

#### `Compze.Teventive.Taggregates.BaseClasses.Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.RemovableEntity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>`

- changed: `public abstract class Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.RemovableEntity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter> : TeventiveRemovableEntity<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>, ITeventiveInternals<TEntityTevent, TEntityTeventImplementation> where TEntity : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.RemovableEntity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter> where TEntityId : struct where TEntityTevent : class, TTaggregateTevent where TEntityTeventImplementation : TTaggregateTeventImplementation, TEntityTevent where TEntityCreatedTevent : TEntityTevent where TEntityRemovedTevent : TEntityTevent where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>`
  to: `public abstract class Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.RemovableEntity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter> : TeventiveRemovableEntity<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>, ITeventRoutedMember<TEntityTevent>, ITeventiveInternals<TEntityTevent, TEntityTeventImplementation>, ITeventRouted where TEntity : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>.RemovableEntity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter> where TEntityId : struct where TEntityTevent : class, TTaggregateTevent where TEntityTeventImplementation : TTaggregateTeventImplementation, TEntityTevent where TEntityCreatedTevent : TEntityTevent where TEntityRemovedTevent : TEntityTevent where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>`

#### `Compze.Teventive.Taggregates.BaseClasses.TaggregateIdentifyingTevent`

- added: `public static ITaggregateTevent<ITaggregateTeventWithIdentity> WrapIn(Type wrapperTeventImplementation, ITaggregateTeventWithIdentity tevent)`

#### `Compze.Teventive.Taggregates.BaseClasses.Tentity<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityId, TTentityTeventImplementation, TTentityTevent, TTentityCreatedTevent, TTentityTeventIdGetterSetter>`

- changed: `public abstract class Tentity<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityId, TTentityTeventImplementation, TTentityTevent, TTentityCreatedTevent, TTentityTeventIdGetterSetter> : TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityTevent, TTentityTeventImplementation>, ITeventiveInternals<TTentityTevent, TTentityTeventImplementation> where TParent : ITeventiveInternals<TParentTevent, TParentTeventImplementation> where TParentTevent : class, ITaggregateTevent where TParentTeventImplementation : TaggregateTevent, TParentTevent where TTentity : Tentity<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityId, TTentityTeventImplementation, TTentityTevent, TTentityCreatedTevent, TTentityTeventIdGetterSetter> where TTentityId : struct where TTentityTeventImplementation : TParentTeventImplementation, TTentityTevent where TTentityTevent : class, TParentTevent where TTentityCreatedTevent : TTentityTevent where TTentityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TTentityId, TTentityTeventImplementation, TTentityTevent>`
  to: `public abstract class Tentity<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityId, TTentityTeventImplementation, TTentityTevent, TTentityCreatedTevent, TTentityTeventIdGetterSetter> : TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityTevent, TTentityTeventImplementation>, ITeventRoutedMember<TTentityTevent>, ITeventiveInternals<TTentityTevent, TTentityTeventImplementation>, ITeventRouted where TParent : ITeventiveInternals<TParentTevent, TParentTeventImplementation> where TParentTevent : class, ITaggregateTevent where TParentTeventImplementation : TaggregateTevent, TParentTevent where TTentity : Tentity<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityId, TTentityTeventImplementation, TTentityTevent, TTentityCreatedTevent, TTentityTeventIdGetterSetter> where TTentityId : struct where TTentityTeventImplementation : TParentTeventImplementation, TTentityTevent where TTentityTevent : class, TParentTevent where TTentityCreatedTevent : TTentityTevent where TTentityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TTentityId, TTentityTeventImplementation, TTentityTevent>`
- removed: `protected Tentity(TParent taggregate, TeventDispatcherConfig? teventAppliersDispatcherConfig = null)`
- added: `protected static TeventRouting<TTentity, TTentityTevent> DeclareAppliers(Action<TeventRouting<TTentity, TTentityTevent>.Declaration> declare)`
- added: `protected Tentity(TParent taggregate)`

#### `Compze.Teventive.Taggregates.BaseClasses.Tentity<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityId, TTentityTeventImplementation, TTentityTevent, TTentityCreatedTevent, TTentityTeventIdGetterSetter>.ICollectionManager`

- changed: `public interface Tentity<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityId, TTentityTeventImplementation, TTentityTevent, TTentityCreatedTevent, TTentityTeventIdGetterSetter>.ICollectionManager : IEntityCollectionManager<TTentity, TTentityId, TTentityTevent, TTentityTeventImplementation, TTentityCreatedTevent>`
  to: `public interface Tentity<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityId, TTentityTeventImplementation, TTentityTevent, TTentityCreatedTevent, TTentityTeventIdGetterSetter>.ICollectionManager : IEntityCollectionManager<TTentity, TTentityId, TTentityTevent, TTentityTeventImplementation, TTentityCreatedTevent>, ITeventRoutedMember<TTentityTevent>`

#### `Compze.Teventive.Taggregates.BaseClasses.TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>`

- changed: `public abstract class TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation> : ITeventiveInternals<TComponentTevent, TComponentTeventImplementation> where TParent : ITeventiveInternals<TParentTevent, TParentTeventImplementation> where TParentTevent : class, ITaggregateTevent where TParentTeventImplementation : TaggregateTevent, TParentTevent where TComponent : TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation> where TComponentTevent : class, TParentTevent where TComponentTeventImplementation : TParentTeventImplementation, TComponentTevent`
  to: `public abstract class TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation> : TeventApplyingComponent<TComponent, TComponentTevent>, ITeventRoutedMember<TComponentTevent>, ITeventiveInternals<TComponentTevent, TComponentTeventImplementation>, ITeventRouted where TParent : ITeventiveInternals<TParentTevent, TParentTeventImplementation> where TParentTevent : class, ITaggregateTevent where TParentTeventImplementation : TaggregateTevent, TParentTevent where TComponent : TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation> where TComponentTevent : class, TParentTevent where TComponentTeventImplementation : TParentTeventImplementation, TComponentTevent`
- removed: `protected ITeventSubscriber<TComponentTevent> RegisterTeventAppliers()`
- removed: `protected TeventiveComponent(TParent parent, bool registerTeventAppliers = true, TeventDispatcherConfig? teventAppliersDispatcherConfig = null)`
- added: `protected TeventiveComponent(TParent parent)`

#### `Compze.Teventive.Taggregates.BaseClasses.TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.Component<TEcComponent, TEcComponentTeventImplementation, TEcComponentTevent>`

- changed: `public abstract class TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.Component<TEcComponent, TEcComponentTeventImplementation, TEcComponentTevent> : TeventiveComponent<TComponent, TComponentTevent, TComponentTeventImplementation, TEcComponent, TEcComponentTevent, TEcComponentTeventImplementation>, ITeventiveInternals<TEcComponentTevent, TEcComponentTeventImplementation> where TEcComponent : TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.Component<TEcComponent, TEcComponentTeventImplementation, TEcComponentTevent> where TEcComponentTeventImplementation : TComponentTeventImplementation, TEcComponentTevent where TEcComponentTevent : class, TComponentTevent`
  to: `public abstract class TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.Component<TEcComponent, TEcComponentTeventImplementation, TEcComponentTevent> : TeventiveComponent<TComponent, TComponentTevent, TComponentTeventImplementation, TEcComponent, TEcComponentTevent, TEcComponentTeventImplementation>, ITeventRoutedMember<TEcComponentTevent>, ITeventiveInternals<TEcComponentTevent, TEcComponentTeventImplementation>, ITeventRouted where TEcComponent : TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.Component<TEcComponent, TEcComponentTeventImplementation, TEcComponentTevent> where TEcComponentTeventImplementation : TComponentTeventImplementation, TEcComponentTevent where TEcComponentTevent : class, TComponentTevent`

#### `Compze.Teventive.Taggregates.BaseClasses.TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.Entity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityTeventIdGetterSetter>`

- changed: `public abstract class TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.Entity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityTeventIdGetterSetter> : Tentity<TComponent, TComponentTevent, TComponentTeventImplementation, TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>, ITeventiveInternals<TEntityTevent, TEntityTeventImplementation> where TEntity : TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.Entity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityTeventIdGetterSetter> where TEntityId : struct where TEntityTevent : class, TComponentTevent where TEntityTeventImplementation : TComponentTeventImplementation, TEntityTevent where TEntityCreatedTevent : TEntityTevent where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>`
  to: `public abstract class TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.Entity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityTeventIdGetterSetter> : Tentity<TComponent, TComponentTevent, TComponentTeventImplementation, TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>, ITeventRoutedMember<TEntityTevent>, ITeventiveInternals<TEntityTevent, TEntityTeventImplementation>, ITeventRouted where TEntity : TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.Entity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityTeventIdGetterSetter> where TEntityId : struct where TEntityTevent : class, TComponentTevent where TEntityTeventImplementation : TComponentTeventImplementation, TEntityTevent where TEntityCreatedTevent : TEntityTevent where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>`

#### `Compze.Teventive.Taggregates.BaseClasses.TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.RemovableEntity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>`

- changed: `public abstract class TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.RemovableEntity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter> : TeventiveRemovableEntity<TComponent, TComponentTevent, TComponentTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>, ITeventiveInternals<TEntityTevent, TEntityTeventImplementation> where TEntity : TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.RemovableEntity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter> where TEntityId : struct where TEntityTevent : class, TComponentTevent where TEntityTeventImplementation : TComponentTeventImplementation, TEntityTevent where TEntityCreatedTevent : TEntityTevent where TEntityRemovedTevent : TEntityTevent where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>`
  to: `public abstract class TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.RemovableEntity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter> : TeventiveRemovableEntity<TComponent, TComponentTevent, TComponentTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>, ITeventRoutedMember<TEntityTevent>, ITeventiveInternals<TEntityTevent, TEntityTeventImplementation>, ITeventRouted where TEntity : TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>.RemovableEntity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter> where TEntityId : struct where TEntityTevent : class, TComponentTevent where TEntityTeventImplementation : TComponentTeventImplementation, TEntityTevent where TEntityCreatedTevent : TEntityTevent where TEntityRemovedTevent : TEntityTevent where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>`

#### `Compze.Teventive.Taggregates.BaseClasses.TeventiveRemovableEntity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>`

- changed: `public abstract class TeventiveRemovableEntity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter> : Tentity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>, ITeventiveInternals<TEntityTevent, TEntityTeventImplementation> where TParent : ITeventiveInternals<TParentTevent, TParentTeventImplementation> where TParentTevent : class, ITaggregateTevent where TParentTeventImplementation : TaggregateTevent, TParentTevent where TEntity : TeventiveRemovableEntity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter> where TEntityId : struct where TEntityTevent : class, TParentTevent where TEntityTeventImplementation : TParentTeventImplementation, TEntityTevent where TEntityCreatedTevent : TEntityTevent where TEntityRemovedTevent : TEntityTevent where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>`
  to: `public abstract class TeventiveRemovableEntity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter> : Tentity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>, ITeventRoutedMember<TEntityTevent>, ITeventiveInternals<TEntityTevent, TEntityTeventImplementation>, ITeventRouted where TParent : ITeventiveInternals<TParentTevent, TParentTeventImplementation> where TParentTevent : class, ITaggregateTevent where TParentTeventImplementation : TaggregateTevent, TParentTevent where TEntity : TeventiveRemovableEntity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter> where TEntityId : struct where TEntityTevent : class, TParentTevent where TEntityTeventImplementation : TParentTeventImplementation, TEntityTevent where TEntityCreatedTevent : TEntityTevent where TEntityRemovedTevent : TEntityTevent where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>`
- added: `protected static TeventRouting<TEntity, TEntityTevent> DeclareAppliers(Action<TeventRouting<TEntity, TEntityTevent>.Declaration> declare)`

#### `Compze.Teventive.Taggregates.Tevents.ITaggregateCreatedTevent`

- changed: `public interface ITaggregateCreatedTevent : ITaggregateTevent, IExactlyOnceTevent, IRemotableTevent, ITevent, IExactlyOnceTessage, IMustBeSentAndHandledTransactionally, IMustBeSentTransactionally, IAtMostOnceTessage, IMustBeHandledTransactionally, IAtLeastOnceTessage, ITessageWithIdentity, IRemotableTessage, ITessage`
  to: `public interface ITaggregateCreatedTevent : ITaggregateTevent, ITevent, ITessage`

#### `Compze.Teventive.Taggregates.Tevents.ITaggregateDeletedTevent`

- changed: `public interface ITaggregateDeletedTevent : ITaggregateTevent, IExactlyOnceTevent, IRemotableTevent, ITevent, IExactlyOnceTessage, IMustBeSentAndHandledTransactionally, IMustBeSentTransactionally, IAtMostOnceTessage, IMustBeHandledTransactionally, IAtLeastOnceTessage, ITessageWithIdentity, IRemotableTessage, ITessage`
  to: `public interface ITaggregateDeletedTevent : ITaggregateTevent, ITevent, ITessage`

#### `Compze.Teventive.Taggregates.Tevents.ITaggregateTevent`

- changed: `public interface ITaggregateTevent : IExactlyOnceTevent, IRemotableTevent, ITevent, IExactlyOnceTessage, IMustBeSentAndHandledTransactionally, IMustBeSentTransactionally, IAtMostOnceTessage, IMustBeHandledTransactionally, IAtLeastOnceTessage, ITessageWithIdentity, IRemotableTessage, ITessage`
  to: `public interface ITaggregateTevent : ITevent, ITessage`

#### `Compze.Teventive.Taggregates.Tevents.TaggregateTevent`

- changed: `public abstract class TaggregateTevent : IMutableTaggregateTevent, ITaggregateTevent, IExactlyOnceTevent, IRemotableTevent, ITevent, IExactlyOnceTessage, IMustBeSentAndHandledTransactionally, IMustBeSentTransactionally, IAtMostOnceTessage, IMustBeHandledTransactionally, IAtLeastOnceTessage, ITessageWithIdentity, IRemotableTessage, ITessage`
  to: `public abstract class TaggregateTevent : IMutableTaggregateTevent, ITaggregateTevent, ITevent, ITessage`
- removed: `public TessageId Id { get; }`

## Compze.Teventive.StateStore

7 types added, 0 removed, 0 changed; in the types on both sides, 0 members added, 0 removed, 0 changed.

### Added types

- `Compze.Teventive.StateStore.IStateStore`: `public interface IStateStore`
  - `ReloadedTaggregates ReloadFromStorage()`
  - `ReloadedTaggregates ReloadFromStorageAfter(Action changeToTheStorage)`

- `Compze.Teventive.StateStore.ITaggregateFilesInMemory`: `public interface ITaggregateFilesInMemory`
  - `IReadOnlyCollection<string> Paths { get; }`
  - `string ReadAllText(string path)`
  - `void Delete(string path)`
  - `void WriteAllText(string path, string content)`

- `Compze.Teventive.StateStore.ITaggregateIndex<TTaggregate, TKey>`: `public interface ITaggregateIndex<out TTaggregate, in TKey> where TTaggregate : class, ITaggregate`
  - `bool HasAny(TKey key)`
  - `IReadOnlyCollection<ITaggregateReference<out TTaggregate>> Lookup(TKey key)`

- `Compze.Teventive.StateStore.ReloadedTaggregates`: `public sealed class ReloadedTaggregates`
  - `public IReadOnlyList<TaggregateId> Added { get; }`
  - `public IReadOnlyList<TaggregateId> Changed { get; }`
  - `public IReadOnlyList<TaggregateId> Removed { get; }`

- `Compze.Teventive.StateStore.StateStoreBuilder`: `public sealed class StateStoreBuilder`
  - `public StateStoreBuilder Index<TTaggregate, TKey>(Func<TTaggregate, IEnumerable<TKey>> keysOf) where TTaggregate : class, ITaggregate where TKey : notnull`
  - `public StateStoreBuilder StoredInFiles(DirectoryInfo folder, JsonSerializerOptions options, Action<TaggregateFiles> declare)`
  - `public StateStoreBuilder StoredInFiles<TService>(Func<TService, DirectoryInfo> folderWith, JsonSerializerOptions options, Action<TaggregateFiles> declare) where TService : class`
  - `public StateStoreBuilder Taggregate<TTaggregate, TSnapshot, TService>(Func<TService, ISnapshotable<TTaggregate, TSnapshot>> createEmpty, Func<TSnapshot, TSnapshot, bool>? snapshotsAreEqual = null) where TTaggregate : class, ITaggregate, ISnapshotable<TTaggregate, TSnapshot> where TService : class`
  - `public StateStoreBuilder Taggregate<TTaggregate, TSnapshot>(Func<ISnapshotable<TTaggregate, TSnapshot>> createEmpty, Func<TSnapshot, TSnapshot, bool>? snapshotsAreEqual = null) where TTaggregate : class, ITaggregate, ISnapshotable<TTaggregate, TSnapshot>`

- `Compze.Teventive.StateStore.StateStoreRegistrar`: `public static class StateStoreRegistrar`
  - `public static IComponentRegistrar StateStore(this IComponentRegistrar @this, Action<StateStoreBuilder> declare)`

- `Compze.Teventive.StateStore.TaggregateFiles`: `public sealed class TaggregateFiles`
  - `public TaggregateFiles KeptInMemory()`
  - `public TaggregateFiles Of<TTaggregate>(string folder, Func<TTaggregate, DirectoryInfo, FileInfo>? fileOf = null) where TTaggregate : class, ITaggregate`

## Compze.Teventive.StateStore.MemoryPack

1 types added, 0 removed, 0 changed; in the types on both sides, 0 members added, 0 removed, 0 changed.

### Added types

- `Compze.Teventive.StateStore.MemoryPack.TaggregateFilesMemoryPackCE`: `public static class TaggregateFilesMemoryPackCE`

## Compze.Teventive.TeventStore

2 types added, 12 removed, 5 changed; in the types on both sides, 12 members added, 14 removed, 2 changed.

### Added types

- `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>.ICollectionManager`: `public interface SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>.ICollectionManager : ITeventRoutedMember<TEntityTevent>`
  - `IReadOnlyEntityCollection<TEntity, TEntityId> Entities { get; }`

- `Compze.Teventive.TeventStore.Wiring.TeventStoreBuilder`: `public sealed class TeventStoreBuilder`
  - `public TeventStoreBuilder Taggregate<TTaggregate>() where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>`

### Removed types

- `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.IQueryModelEntityCollectionManager<TEntity, TEntityId>`: `public interface IQueryModelEntityCollectionManager<TEntity, in TEntityId> where TEntity : class`
- `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.IReadonlyQueryModelEntityCollection<TEntity, TEntityId>`: `public interface IReadonlyQueryModelEntityCollection<TEntity, in TEntityId> : IEnumerable<TEntity>, IEnumerable where TEntity : class`
- `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.QueryModelEntityCollection<TEntity, TEntityId>`: `public class QueryModelEntityCollection<TEntity, TEntityId> : IReadonlyQueryModelEntityCollection<TEntity, TEntityId>, IEnumerable<TEntity>, IEnumerable where TEntity : class where TEntityId : notnull`
- `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.NestedComponent<TNestedComponent, TNestedComponentTevent>`: `public abstract class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.NestedComponent<TNestedComponent, TNestedComponentTevent> : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TNestedComponent, TNestedComponentTevent> where TNestedComponent : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.NestedComponent<TNestedComponent, TNestedComponentTevent> where TNestedComponentTevent : class, TComponentTevent`
- `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.NestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>`: `public abstract class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.NestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter> : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.NestedComponent<TEntity, TEntityTevent> where TEntity : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.NestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter> where TEntityId : struct where TEntityTevent : class, TComponentTevent where TEntityCreatedTevent : TEntityTevent where TTeventEntityIdGetter : IGetTaggregateEntityTeventEntityId<TEntityTevent, TEntityId>`
- `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.NestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>.CollectionManager`: `public class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.NestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>.CollectionManager : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TEntity, TEntityTevent>.QueryModelEntityCollectionManager<TComponent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>, IQueryModelEntityCollectionManager<TEntity, TEntityId>`
- `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.QueryModelEntityCollectionManager<TParent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>`: `public class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.QueryModelEntityCollectionManager<TParent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter> : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.QueryModelEntityCollectionManager<TParent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>, IQueryModelEntityCollectionManager<TEntity, TEntityId> where TEntity : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TEntity, TEntityTevent> where TEntityId : notnull where TEntityTevent : class, TTaggregateTevent where TEntityCreatedTevent : TEntityTevent where TEntityRemovedTevent : TEntityTevent where TTeventEntityIdGetter : IGetTaggregateEntityTeventEntityId<TEntityTevent, TEntityId>`
- `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.QueryModelEntityCollectionManager<TParent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>`: `public class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.QueryModelEntityCollectionManager<TParent, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter> : IQueryModelEntityCollectionManager<TEntity, TEntityId> where TEntity : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TEntity, TEntityTevent> where TEntityId : notnull where TEntityTevent : class, TTaggregateTevent where TEntityCreatedTevent : TEntityTevent where TEntityTeventIdGetterSetter : IGetTaggregateEntityTeventEntityId<TEntityTevent, TEntityId>`
- `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.RemovableNestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>`: `public abstract class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.RemovableNestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter> : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.NestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter> where TEntity : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.NestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter> where TEntityId : struct where TEntityTevent : class, TComponentTevent where TEntityCreatedTevent : TEntityTevent where TEntityRemovedTevent : TEntityTevent where TTeventEntityIdGetter : IGetTaggregateEntityTeventEntityId<TEntityTevent, TEntityId>`
- `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.RemovableNestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>.ICollectionManager`: `public interface SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>.RemovableNestedEntity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>.ICollectionManager : IQueryModelEntityCollectionManager<TEntity, TEntityId>`
- `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>.CollectionManager`: `public class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>.CollectionManager : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TEntity, TEntityTevent>.QueryModelEntityCollectionManager<TQueryModel, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>, IQueryModelEntityCollectionManager<TEntity, TEntityId>`
- `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>.CollectionManager`: `public class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>.CollectionManager : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TEntity, TEntityTevent>.QueryModelEntityCollectionManager<TQueryModel, TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>, IQueryModelEntityCollectionManager<TEntity, TEntityId>`

### Changed types

#### `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>`

- changed: `public abstract class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent> : VersionedEntity<TQueryModel>, IEntity, IEntity<Guid> where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent> where TTaggregateTevent : class, ITaggregateTevent`
  to: `public abstract class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent> : TeventApplyingRoot<TQueryModel, TTaggregateTevent>, IEntity, IEntity<Guid>, ITeventRouted where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent> where TTaggregateTevent : class, ITaggregateTevent`
- removed: `protected ITeventSubscriber<TTaggregateTevent> RegisterTeventAppliers()`
- removed: `protected SelfGeneratingQueryModel(TeventDispatcherConfig? teventAppliersDispatcherConfig = null)`
- removed: `protected virtual void AssertInvariantsAreMet()`
- added: `protected SelfGeneratingQueryModel() (implicit)`

#### `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent>`

- changed: `public abstract class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent> where TComponent : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent> where TComponentTevent : class, TTaggregateTevent`
  to: `public abstract class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent> : TeventApplyingComponent<TComponent, TComponentTevent>, ITeventRoutedMember<TComponentTevent>, ITeventRouted where TComponent : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TComponent, TComponentTevent> where TComponentTevent : class, TTaggregateTevent`
- removed: `protected Component(ITeventSubscriber<TComponentTevent> appliersSubscriber, bool registerTeventAppliers, TeventDispatcherConfig? teventAppliersDispatcherConfig = null)`
- removed: `protected Component(TQueryModel queryModel)`
- removed: `protected ITeventSubscriber<TComponentTevent> RegisterTeventAppliers()`
- added: `protected Component(ITeventRouted parent)`

#### `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>`

- changed: `public abstract class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter> : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TEntity, TEntityTevent> where TEntity : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter> where TEntityId : struct where TEntityTevent : class, TTaggregateTevent where TEntityCreatedTevent : TEntityTevent where TTeventEntityIdGetter : IGetTaggregateEntityTeventEntityId<TEntityTevent, TEntityId>`
  to: `public abstract class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter> : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Component<TEntity, TEntityTevent>, ITeventRoutedMember<TEntityTevent>, ITeventRouted where TEntity : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter> where TEntityId : struct where TEntityTevent : class, TTaggregateTevent where TEntityCreatedTevent : TEntityTevent where TTeventEntityIdGetter : IGetTaggregateEntityTeventEntityId<TEntityTevent, TEntityId>`
- removed: `protected Entity(TQueryModel queryModel, TeventDispatcherConfig? teventAppliersDispatcherConfig = null)`
- removed: `public static SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>.CollectionManager CreateSelfManagingCollection(TQueryModel parent)`
- added: `protected Entity() (implicit)`
- added: `protected static TeventRouting<TEntity, TEntityTevent> DeclareAppliers(Action<TeventRouting<TEntity, TEntityTevent>.Declaration> declare)`
- added: `public static SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>.ICollectionManager CreateSelfManagingCollection(ITeventRouted parent)`

#### `Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels.SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>`

- changed: `public abstract class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter> : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter> where TEntity : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter> where TEntityId : struct where TEntityTevent : class, TTaggregateTevent where TEntityCreatedTevent : TEntityTevent where TEntityRemovedTevent : TEntityTevent where TTeventEntityIdGetter : IGetTaggregateEntityTeventEntityId<TEntityTevent, TEntityId>`
  to: `public abstract class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter> : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>, ITeventRoutedMember<TEntityTevent>, ITeventRouted where TEntity : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter> where TEntityId : struct where TEntityTevent : class, TTaggregateTevent where TEntityCreatedTevent : TEntityTevent where TEntityRemovedTevent : TEntityTevent where TTeventEntityIdGetter : IGetTaggregateEntityTeventEntityId<TEntityTevent, TEntityId>`
- removed: `protected Entity(TQueryModel queryModel)`
- removed: `public static SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TTeventEntityIdGetter>.CollectionManager CreateSelfManagingCollection(TQueryModel parent)`
- added: `protected Entity() (implicit)`
- added: `protected static TeventRouting<TEntity, TEntityTevent> DeclareAppliers(Action<TeventRouting<TEntity, TEntityTevent>.Declaration> declare)`
- added: `public static SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>.ICollectionManager CreateSelfManagingCollection(ITeventRouted parent)`

#### `Compze.Teventive.TeventStore.Refactoring.Migrations.TeventMigration<TMigratedTaggregateTeventHierarchyRootInterface>`

- changed: `public abstract class TeventMigration<TMigratedTaggregateTeventHierarchyRootInterface> : ITeventMigration where TMigratedTaggregateTeventHierarchyRootInterface : ITaggregateTevent`
  to: `public abstract class TeventMigration<TMigratedTaggregateTeventHierarchyRootInterface> : ITeventMigration where TMigratedTaggregateTeventHierarchyRootInterface : ITaggregateTeventWithIdentity`

#### `Compze.Teventive.TeventStore.TeventStore`

- changed: `public IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetTaggregateHistory(TaggregateId id)`
  to: `public IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> GetTaggregateHistory(TaggregateId id)`
- changed: `public IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetTaggregateHistoryForUpdate(TaggregateId id)`
  to: `public IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> GetTaggregateHistoryForUpdate(TaggregateId id)`
- removed: `public void SaveSingleTaggregateTevents(IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> wrappedTevents)`
- removed: `public void StreamTevents(int batchSize, Action<IReadOnlyList<ITaggregateTevent<ITaggregateTevent>>> handleTevents)`
- added: `public void SaveSingleTaggregateTevents(IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> wrappedTevents)`
- added: `public void StreamTevents(int batchSize, Action<IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>>> handleTevents)`

#### `Compze.Teventive.TeventStore.Wiring.TeventStoreRegistrar`

- removed: `public static IComponentRegistrar TeventStore(this IComponentRegistrar registrar, string connectionName)`
- removed: `public static IComponentRegistrar TeventStoreForFlexibleTesting(this IComponentRegistrar registrar, string connectionName, Func<IReadOnlyList<ITeventMigration>> migrations)`
- added: `public static IComponentRegistrar TeventStore(this IComponentRegistrar registrar, string connectionName, Action<TeventStoreBuilder>? declare = null)`
- added: `public static IComponentRegistrar TeventStoreForFlexibleTesting(this IComponentRegistrar registrar, string connectionName, Func<IReadOnlyList<ITeventMigration>> migrations, Action<TeventStoreBuilder>? declare = null)`

## Compze.Teventive.TeventStore.Abstractions

0 types added, 1 removed, 1 changed; in the types on both sides, 5 members added, 6 removed, 5 changed.

### Removed types

- `Compze.Teventive.TeventStore.Abstractions.ITeventStoreUpdater`: `public interface ITeventStoreUpdater : IDisposable`

### Changed types

#### `Compze.Teventive.TeventStore.Abstractions.ITeventStore`

- changed: `IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetTaggregateHistory(TaggregateId id)`
  to: `IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> GetTaggregateHistory(TaggregateId id)`
- changed: `IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetTaggregateHistoryForUpdate(TaggregateId id)`
  to: `IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> GetTaggregateHistoryForUpdate(TaggregateId id)`
- removed: `void SaveSingleTaggregateTevents(IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> wrappedTevents)`
- removed: `void StreamTevents(int batchSize, Action<IReadOnlyList<ITaggregateTevent<ITaggregateTevent>>> handleTevents)`
- added: `void SaveSingleTaggregateTevents(IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> wrappedTevents)`
- added: `void StreamTevents(int batchSize, Action<IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>>> handleTevents)`

#### `Compze.Teventive.TeventStore.Abstractions.ITeventStoreReader`

- changed: `public interface ITeventStoreReader`
  to: `public interface ITeventStoreReader : ITaggregateStoreReader`
- changed: `IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetHistory(TaggregateId taggregateId)`
  to: `IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> GetHistory(TaggregateId taggregateId)`
- removed: `TTaggregate GetReadonlyCopy<TTaggregate>(TaggregateId taggregateId) where TTaggregate : class, ITaggregate`
- changed: `TTaggregate GetReadonlyCopyOfVersion<TTaggregate>(TaggregateId taggregateId, int version) where TTaggregate : class, ITaggregate`
  to: `TTaggregate GetReadonlyCopyOfVersion<TTaggregate>(TaggregateId taggregateId, int version) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>`

#### `Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations.ISingleTaggregateInstanceHandlingTeventMigrator`

- removed: `void MigrateTevent(ITaggregateTevent<ITaggregateTevent> wrappedTevent, ITeventModifier modifier)`
- added: `void MigrateTevent(ITaggregateTevent<ITaggregateTeventWithIdentity> wrappedTevent, ITeventModifier modifier)`

#### `Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations.ITeventModifier`

- removed: `void InsertBefore(params ITaggregateTevent<ITaggregateTevent>[] insert)`
- removed: `void Replace(params ITaggregateTevent<ITaggregateTevent>[] wrappedTevents)`
- added: `void InsertBefore(params ITaggregateTevent<ITaggregateTeventWithIdentity>[] insert)`
- added: `void Replace(params ITaggregateTevent<ITaggregateTeventWithIdentity>[] wrappedTevents)`

#### `Compze.Teventive.TeventStore.Abstractions.TeventStoreTestingExtensions`

- changed: `public static IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize(this ITeventStore @this, int batchSize = 10000)`
  to: `public static IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize(this ITeventStore @this, int batchSize = 10000)`

## Compze.Teventive.TeventStore.Typermedia

0 types added, 0 removed, 5 changed; in the types on both sides, 0 members added, 0 removed, 7 changed.

### Changed types

#### `Compze.Teventive.TeventStore.Typermedia.TeventStoreApi`

- changed: `public static void RegisterHandlersForTaggregate<TTaggregate, TTevent>(TypermediaHandlerRegistrar registrar) where TTaggregate : class, ITaggregate<TTevent> where TTevent : ITaggregateTevent`
  to: `public static void RegisterHandlersForTaggregate<TTaggregate, TTevent>(TypermediaHandlerRegistrar registrar) where TTaggregate : class, ITaggregate<TTevent> where TTevent : class, ITaggregateTeventWithIdentity`

#### `Compze.Teventive.TeventStore.Typermedia.TeventStoreApi.TommandApi`

- changed: `public TeventStoreApi.TommandApi.SaveTaggregate<TTaggregate> Save<TTaggregate>(TTaggregate taggregate) where TTaggregate : class, ITaggregate`
  to: `public TeventStoreApi.TommandApi.SaveTaggregate<TTaggregate> Save<TTaggregate>(TTaggregate taggregate) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>`

#### `Compze.Teventive.TeventStore.Typermedia.TeventStoreApi.TommandApi.SaveTaggregate<TTaggregate>`

- changed: `public class TeventStoreApi.TommandApi.SaveTaggregate<TTaggregate> : StrictlyLocal.Tommands.StrictlyLocalTommand, IStrictlyLocalTommand, ITommand, IExactlyOneReceiverTessage, IMustBeSentTransactionally, ITessage, IStrictlyLocalTessage where TTaggregate : class, ITaggregate`
  to: `public class TeventStoreApi.TommandApi.SaveTaggregate<TTaggregate> : StrictlyLocal.Tommands.StrictlyLocalTommand, IStrictlyLocalTommand, ITommand, IExactlyOneReceiverTessage, IMustBeSentTransactionally, ITessage, IStrictlyLocalTessage where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>`

#### `Compze.Teventive.TeventStore.Typermedia.TeventStoreApi.TueryApi`

- changed: `public TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregate<TTaggregate> GetReadOnlyCopy<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate`
  to: `public TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregate<TTaggregate> GetReadOnlyCopy<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>`
- changed: `public TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregateVersion<TTaggregate> GetReadOnlyCopyOfVersion<TTaggregate>(TaggregateId id, int version) where TTaggregate : class, ITaggregate`
  to: `public TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregateVersion<TTaggregate> GetReadOnlyCopyOfVersion<TTaggregate>(TaggregateId id, int version) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>`
- changed: `public TeventStoreApi.TueryApi.GetTaggregateHistory<TTevent> GetHistory<TTevent>(TaggregateId id) where TTevent : ITaggregateTevent`
  to: `public TeventStoreApi.TueryApi.GetTaggregateHistory<TTevent> GetHistory<TTevent>(TaggregateId id) where TTevent : class, ITaggregateTeventWithIdentity`
- changed: `public TeventStoreApi.TueryApi.TaggregateLink<TTaggregate> GetForUpdate<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate`
  to: `public TeventStoreApi.TueryApi.TaggregateLink<TTaggregate> GetForUpdate<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>`

#### `Compze.Teventive.TeventStore.Typermedia.TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregate<TTaggregate>`

- changed: `public class TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregate<TTaggregate> : StrictlyLocal.Tueries.StrictlyLocalTuery<TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregate<TTaggregate>, TTaggregate>, IStrictlyLocalTuery<TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregate<TTaggregate>, TTaggregate>, ITuery<TTaggregate>, ITuery, ITypermediaTessage<TTaggregate>, ITypermediaTessage, ICannotBeSentRemotelyFromWithinTransaction, IExactlyOneReceiverTessage, ITessage, IStrictlyLocalTessage where TTaggregate : class, ITaggregate`
  to: `public class TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregate<TTaggregate> : StrictlyLocal.Tueries.StrictlyLocalTuery<TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregate<TTaggregate>, TTaggregate>, IStrictlyLocalTuery<TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregate<TTaggregate>, TTaggregate>, ITuery<TTaggregate>, ITuery, ITypermediaTessage<TTaggregate>, ITypermediaTessage, ICannotBeSentRemotelyFromWithinTransaction, IExactlyOneReceiverTessage, ITessage, IStrictlyLocalTessage where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>`

#### `Compze.Teventive.TeventStore.Typermedia.TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregateVersion<TTaggregate>`

- changed: `public class TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregateVersion<TTaggregate> : StrictlyLocal.Tueries.StrictlyLocalTuery<TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregateVersion<TTaggregate>, TTaggregate>, IStrictlyLocalTuery<TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregateVersion<TTaggregate>, TTaggregate>, ITuery<TTaggregate>, ITuery, ITypermediaTessage<TTaggregate>, ITypermediaTessage, ICannotBeSentRemotelyFromWithinTransaction, IExactlyOneReceiverTessage, ITessage, IStrictlyLocalTessage where TTaggregate : class, ITaggregate`
  to: `public class TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregateVersion<TTaggregate> : StrictlyLocal.Tueries.StrictlyLocalTuery<TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregateVersion<TTaggregate>, TTaggregate>, IStrictlyLocalTuery<TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregateVersion<TTaggregate>, TTaggregate>, ITuery<TTaggregate>, ITuery, ITypermediaTessage<TTaggregate>, ITypermediaTessage, ICannotBeSentRemotelyFromWithinTransaction, IExactlyOneReceiverTessage, ITessage, IStrictlyLocalTessage where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>`

#### `Compze.Teventive.TeventStore.Typermedia.TeventStoreApi.TueryApi.GetTaggregateHistory<TTevent>`

- changed: `public class TeventStoreApi.TueryApi.GetTaggregateHistory<TTevent> : StrictlyLocal.Tueries.StrictlyLocalTuery<TeventStoreApi.TueryApi.GetTaggregateHistory<TTevent>, IEnumerable<ITaggregateTevent<TTevent>>>, IStrictlyLocalTuery<TeventStoreApi.TueryApi.GetTaggregateHistory<TTevent>, IEnumerable<ITaggregateTevent<TTevent>>>, ITuery<IEnumerable<ITaggregateTevent<TTevent>>>, ITuery, ITypermediaTessage<IEnumerable<ITaggregateTevent<TTevent>>>, ITypermediaTessage, ICannotBeSentRemotelyFromWithinTransaction, IExactlyOneReceiverTessage, ITessage, IStrictlyLocalTessage where TTevent : ITaggregateTevent`
  to: `public class TeventStoreApi.TueryApi.GetTaggregateHistory<TTevent> : StrictlyLocal.Tueries.StrictlyLocalTuery<TeventStoreApi.TueryApi.GetTaggregateHistory<TTevent>, IEnumerable<ITaggregateTevent<TTevent>>>, IStrictlyLocalTuery<TeventStoreApi.TueryApi.GetTaggregateHistory<TTevent>, IEnumerable<ITaggregateTevent<TTevent>>>, ITuery<IEnumerable<ITaggregateTevent<TTevent>>>, ITuery, ITypermediaTessage<IEnumerable<ITaggregateTevent<TTevent>>>, ITypermediaTessage, ICannotBeSentRemotelyFromWithinTransaction, IExactlyOneReceiverTessage, ITessage, IStrictlyLocalTessage where TTevent : class, ITaggregateTeventWithIdentity`

#### `Compze.Teventive.TeventStore.Typermedia.TeventStoreApi.TueryApi.TaggregateLink<TTaggregate>`

- changed: `public class TeventStoreApi.TueryApi.TaggregateLink<TTaggregate> : StrictlyLocal.Tueries.StrictlyLocalTuery<TeventStoreApi.TueryApi.TaggregateLink<TTaggregate>, TTaggregate>, IStrictlyLocalTuery<TeventStoreApi.TueryApi.TaggregateLink<TTaggregate>, TTaggregate>, ITuery<TTaggregate>, ITuery, ITypermediaTessage<TTaggregate>, ITypermediaTessage, ICannotBeSentRemotelyFromWithinTransaction, IExactlyOneReceiverTessage, ITessage, IStrictlyLocalTessage where TTaggregate : class, ITaggregate`
  to: `public class TeventStoreApi.TueryApi.TaggregateLink<TTaggregate> : StrictlyLocal.Tueries.StrictlyLocalTuery<TeventStoreApi.TueryApi.TaggregateLink<TTaggregate>, TTaggregate>, IStrictlyLocalTuery<TeventStoreApi.TueryApi.TaggregateLink<TTaggregate>, TTaggregate>, ITuery<TTaggregate>, ITuery, ITypermediaTessage<TTaggregate>, ITypermediaTessage, ICannotBeSentRemotelyFromWithinTransaction, IExactlyOneReceiverTessage, ITessage, IStrictlyLocalTessage where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>`

#### `Compze.Teventive.TeventStore.Typermedia.TeventStoreRegistrationBuilder`

- changed: `public TeventStoreRegistrationBuilder HandleTaggregate<TTaggregate, TTevent>() where TTaggregate : class, ITaggregate<TTevent> where TTevent : ITaggregateTevent`
  to: `public TeventStoreRegistrationBuilder HandleTaggregate<TTaggregate, TTevent>() where TTaggregate : class, ITaggregate<TTevent> where TTevent : class, ITaggregateTeventWithIdentity`

## No public API change

Changed files, but not their public API: Compze.DependencyInjection, Compze.Serialization.Newtonsoft, Compze.Teventive.TeventStore.MicrosoftSql, Compze.Teventive.TeventStore.MySql, Compze.Teventive.TeventStore.PostgreSql, Compze.Teventive.TeventStore.Sqlite.
