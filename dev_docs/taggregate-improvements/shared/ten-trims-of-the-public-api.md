# Ten trims of the public API: the end review's tenth question, specified

**Decided by Magnus, 2026-11-18: OK to all.** The end review's tenth question (item 75) put ten small trims to him as a
batch, each recommended by one reviewer or both: [the consolidation](reviews/end-review-consolidated.md), Q10. Specified
here before it is built, on the spike after the routing change of the ninth question.

Each trim takes from the public API what a consumer doesn't need, or what tells less than the truth. None changes what the
stores do.

## The ten, and what a consumer writes instead

1. **`Taggregate.AssertCanBeChanged()` is private.** It was for a class that changes its state other than by applying
   tevents, while its parts are moved to tevents; JAStudio's classes no longer do. Publishing on an instance a store serves
   to readers still throws, saying it is read only.
   - Instead: change state by publishing tevents.
2. **`ITaggregateStoreUpdater.Save<TTaggregate>(taggregate)` is `Add(taggregate)`, without the type parameter.** It only
   adds a new taggregate, in both stores; a change is saved by the commit. The store is chosen by the taggregate's class,
   so the type parameter told the updater nothing.
   - Instead of `taggregates.Save(vocab)` or `taggregates.Save<IEntity>(vocab)`: `taggregates.Add(vocab)`.
   - The stores' internal sessions follow: `ITaggregateStoreSession.Add`. So do the stores' messages and documentation, and
     the specifications' names: "after a unit of work adds a vocab".
   - Not renamed here: the tevent store's typermedia tommand, `TeventStoreApi.Tommands.Save` and `SaveTaggregate`, which
     now calls `Add`, and the tevent store's exceptions named for saving, for the naming pass.
3. **`ITaggregateIndex.HasAny` is removed.** It is `Lookup(key).Count > 0`, at the same cost.
   - Instead of `byQuestion.HasAny("taberu")`: `byQuestion.Lookup("taberu").Count > 0`.
4. **`ITaggregateReference.ForUpdate()` is removed.** It is a second way to get a taggregate for update, which JAStudio
   never uses; without it a reference only reads, and the unit of work's updater is the one way to change a taggregate.
   - Instead of `reference.ForUpdate()`: `updater.GetForUpdate<TTaggregate>(reference.Id)`.
5. **`SnapshotRoundTrip.AssertLosesNothing` is removed; `DifferencesOf` stays.** It is "throw if `DifferencesOf` isn't
   empty", one line a consumer writes with its own assertions.
   - Instead: `SnapshotRoundTrip.DifferencesOf(taggregate, createEmpty).Must().BeEmpty()`, or the consumer's own
     assertion library's equivalent. The differences are lines that name each path, so the failure says what was lost.
6. **`IMutableTaggregateTevent` and `IMutableTaggregateTeventWithIdentity` are internal.** Their members, marked for
   internal use, set a tevent's taggregate id, version, time stamp and id: what a taggregate does when it publishes, and a
   tevent store when it reads tevents back. They move to `Compze.Teventive.Taggregates.Tevents._internal`, and lose the
   `[Obsolete]` marks that only stood in for being internal.
   - Instead: nothing. A tevent gets those values by being published on a taggregate.
   - Compze's own specifications that build tevents with a given version, id or time stamp, for the tevent store's
     specifications and the migrations', set them through one helper of the specifications, `TaggregateTeventCE` in
     Compze.Tests.Common, by reflection on the properties' private setters, marked `review:`. Compze.Teventive grants its
     internals to Compze.Serialization.Newtonsoft.InternalSpecifications, whose specifications set them through the internal
     interface.
7. **`ITeventRoutedMember<TTevent>` is `ITeventRoutedPart<TTevent>`.** Everything around it says "part": its own summary,
   `TeventRouting.AssertRoutesToAPart`, the messages, the design.
8. **`SharedTomponentSlotOfAClass<…>` is `SharedTomponentSlot<…>`, and its constructor takes
   `Func<TOwner, SharedTomponent<TTomponentTevent>>`.** "Of a class" told it apart from the per-instance slot, which is gone.
   Its constructor took an internal interface, then checked at run time that what it got was a `SharedTomponent`; the
   parameter's type now says so, and the check goes. The check that the tomponent was constructed with this slot stays.
   - Instead of `new SharedTomponentSlotOfAClass<…>(typeof(SeeAlsoTevent<>), vocab => vocab._seeAlso)`:
     `new SharedTomponentSlot<…>(typeof(SeeAlsoTevent<>), vocab => vocab._seeAlso)`, with the same type arguments.
   - The internal interface it implements follows: `ISharedTomponentSlot`.
9. **`ReloadedTaggregates`' `Added`, `Changed` and `Removed` are `IReadOnlyCollection<TaggregateId>`.** They have no
   order, and a list's positions read as one: an API spends nothing on what callers can do as well themselves, and a
   caller that wants an order sorts.
10. **One reload member: `IStateStore.ReloadFromStorage(Action? changeToTheStorage = null)`** replaces
    `ReloadFromStorage()` and `ReloadFromStorageAfter(Action)`.
    - Instead of `store.ReloadFromStorageAfter(() => …)`: `store.ReloadFromStorage(() => …)`. `store.ReloadFromStorage()`
      is unchanged.

Names beyond these wait for the naming pass, the eleventh question.

## Specifications

What each removed member was specified for is specified through what replaces it:

- **1:** the specification of a change other than by a tevent to a reader's instance goes, with its test vocab's method.
  In its place: after a tevent is published on the instance the reader gets of an added vocab, the publish throws, saying
  that the instance is read only, and the vocab the reader gets keeps the meaning it had.
- **2:** every specification that saved a new taggregate adds it, in both stores.
- **3:** the index's specification that creates a vocab for a question unless one is filed under it looks the question up
  and counts what it finds; its performance specification no longer measures `HasAny`, and measures the lookups as before.
- **4:** the specifications that got a taggregate for update through its reference get it from the unit of work's updater
  by the reference's id, and a reference held throughout still gives the unit of work's copy inside it and the committed
  instance outside; the one that specified that `ForUpdate()` throws outside a unit of work specifies that getting the
  vocab for update through an updater resolved in a scope that is no unit of work throws.
- **5:** the snapshot round trip's specifications assert that `DifferencesOf` is empty where the snapshot loses nothing,
  and that it names each difference where it loses something, as the failures of `AssertLosesNothing` did.
- **6:** the tevent store's, the migrations' and the serializer's specifications build their tevents as before, through
  the helper or the internal interface.
- **7, 8:** the routing's and the shared tomponents' specifications compile with the new names, and the one that hands a
  slot a tomponent another slot constructed still throws.
- **9, 10:** the reload specifications run through the one member, with and without a change to the storage, and compare
  `Added`, `Changed` and `Removed` as collections.
