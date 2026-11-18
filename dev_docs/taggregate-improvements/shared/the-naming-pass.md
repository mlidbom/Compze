# The naming pass: the spike's placeholder names, and the names both reviewers proposed

**Decided by Magnus, 2026-11-18** (the end review's eleventh question, item 77). He approved jastudio-rep-opus's picks,
with one change: `SnapshotRoundTrip.DifferencesOf` becomes `DifferencesBetweenItAndItsCopy`. His words: "Use that. It is
truthful and I don't need to read the doc comment to understand what in the world is meant by Of here. All others
approved." Specified here before it is built.

## How the names were chosen

The same prompt went to two fresh sessions: one with all our rules, user-level and the catalog's, and one with nothing
of ours. The prompt gave the vocabulary, what each name is and how it reads at a call site, and the names the two
reviewers had proposed, and asked for a name or a keep for each. It and both answers are in
`C:\Users\magnu\AppData\Local\Temp\naming-pass-replay`. The two sessions agreed on 13 of the 16 names. Where they
differed, jastudio-rep-opus picked, and Magnus approved the picks:

- **The service of dynamic observers:** `IDynamicTeventObservers` (without our rules), over `IDynamicObservers` (with
  them): "Tevent" in the name, as in `ObserveTevents` and the rest of the observation API.
- **What a load found:** `ChangesFoundInStorage` (with our rules), over `StorageDiff` (without): no abbreviation.
- **Writing every taggregate:** `WriteAllToStorage` (without our rules), over `RewriteStorage` (with them): it pairs
  with `LoadFromStorage`, and "rewrite storage" could read as restructuring the storage.
- **A class's folder:** `FolderOf` (with our rules), over keeping `Of` (without): it says that the string is a folder.

`SelfGeneratingQueryModel<,>.IParent` keeps its name: in a part's constructor, `(IParent parent)`, it reads as what it
is, and the query model it is nested in scopes it.

## The renames

| Now | New |
|---|---|
| `ITaggregateStoreBulkReader.All<T>()` | `ReferencesToAll<T>()` |
| `ITeventObservation` | `IDynamicTeventObservers`, keeping `ObserveTevents` |
| `TeventObservationRegistrar.ForEachUnitOfWork<T>(...)` | `ForTeventsOfEachUnitOfWork<T>(...)` |
| `ReloadedTaggregates` | `ChangesFoundInStorage` |
| `IStateStore.ReloadFromStorage(changeToTheStorage)` | `LoadFromStorage(changeTheStorageFirst)` |
| `IStateStore.WriteEverythingAnew()` | `WriteAllToStorage()` |
| `StateStoreBuilder.StoredInFiles(...)` | `StoredInJsonFiles(...)` |
| `TaggregateFiles` | `TaggregateFilesBuilder` |
| `TaggregateFiles.Of<T>(folder)` | `FolderOf<T>(folder)` |
| `CachedWithMemoryPack()` | `StartingFromAMemoryPackCache()` |
| `AllowedInTransactionsThatAlsoWriteADatabase()` | `AcceptingNonAtomicCommitsWithADatabase()` |
| `SnapshotRoundTrip.DifferencesOf(...)` | `DifferencesBetweenItAndItsCopy(...)` |
| `TeventApplyingComponent<,>` | `TeventApplyingPart<,>`; `TeventApplyingRoot<,>` keeps its name |
| `AddRestored(id)`, on every entity collection | `AddWhileRestoring(id)` |
| the tevent store's tuery `GetReadonlyCopyOfTaggregate<T>`, made by `Tueries.GetReadOnlyCopy<T>(id)` | `GetTaggregate<T>`, made by `Tueries.Get<T>(id)` |
| the tevent store's tommand `SaveTaggregate<T>`, made by `Tommands.Save(taggregate)` | `AddTaggregate<T>`, made by `Tommands.Add(taggregate)` |
| the tevent store's exceptions `AttemptToSaveAlreadyPersistedTaggregateException`, `AttemptToSaveEmptyTaggregateException` | `AttemptToAdd…` |

What a consumer writes, before and after:

```csharp
store.StoredInFiles(folder, options, files => files.Of<VocabNote>("vocab").CachedWithMemoryPack());
var reloaded = stateStore.ReloadFromStorage(() => PullTheCorpus());
foreach(var vocab in bulkReader.All<VocabNote>()) ...
```

```csharp
store.StoredInJsonFiles(folder, options, files => files.StartingFromAMemoryPackCache().FolderOf<VocabNote>("vocab"));
var changes = stateStore.LoadFromStorage(() => PullTheCorpus());
foreach(var vocab in bulkReader.ReferencesToAll<VocabNote>()) ...
```

**The options for all the files stay where they are,** in the same chain as `FolderOf`: `StartingFromAMemoryPackCache`,
`AcceptingNonAtomicCommitsWithADatabase` and `KeptInMemory`. Every example in the documentation writes them first, so
that none reads as an option of the class declared before it. Both sessions would have moved them onto what
`StoredInJsonFiles` returns; that changes the API's shape for what the order of a chain fixes.

**Not renamed here:** `KeptInMemory` and `ITaggregateFilesInMemory` wait for the question about the testing aids
(question 6), and `StateStore`, `IStateStore` and `StateStoreBuilder` for the question about the store's own name
(question 2).

**Internal names follow the public ones:** the storage seam's write of every taggregate is `WriteAll`, the class behind
`IDynamicTeventObservers` is `DynamicTeventObservers`, and the extension class of the MemoryPack package is
`TaggregateFilesBuilderMemoryPackCE`. The specifications' names describe behaviour, such as "the store reloads", and
keep their words where those still tell the truth.

## Specifications

The existing specifications, renamed with the API. A rename changes no behaviour, so the full suite passing is the
specification that the renames lost nothing.
