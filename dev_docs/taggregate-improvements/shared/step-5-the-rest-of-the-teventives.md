# Step 5: the rest of Compze's teventives on the tables, and registration per instance removed: specified

Step 5 of [the order](README.md#the-order-we-propose), approved by Magnus on 2026-11-17 ("let's try step 5 whatever that
may mean"). Written from a survey of the code at c5c910e06. Every name is a placeholder for the naming pass of the
spike's end review.

Two terms, as in [the-design.md](the-design.md):

- **registration per instance:** a teventive builds its own `IMutableTeventDispatcher` in each instance, and registers
  its appliers or tevent handlers on it in its constructor (`RegisterTeventAppliers()`, `RegisterTeventHandlers()`, a part
  registering on its parent, a `TeventDispatcherConfig` handed to a base constructor).
- **a table:** the class's `TeventRouting`, kept in a static field and found through an explicit `ITeventRouted`, as
  built in step 3.

## What step 5 does

- **Every teventive class in Compze routes through tables:** entities, removable entities, shared tentities and their
  collections, and the query models. Step 3 built the root, components, entity collections that remove nothing, slots,
  and inheritance.
- **Registration per instance is removed,** and with it the fallbacks that let a class declare no table.
- **Step 3's remaining safeguards are built:** the table typed on its tevent root, the first table only from Compze's base
  class, matching shared with the dispatcher, a part asserting its parent's table routes to it, and a slot's route
  asserting its tomponent.

**JAStudio:** all 17 of its teventive classes already declare tables, and none uses anything step 5 removes. What changes
for it is mechanical, in one increment (2 below): `TeventRouting<JPNote>.DeclareAppliers(...)` becomes the base class's
`DeclareAppliers(...)`, and each table's type names its tevent root, `TeventRouting<JPNote, INoteTevent>`. I tell
taggregates-opus with the edit when it's built.

## 1. A table starts with its Compze base class's routes

**The problem:** `Extend` requires the direct base class, and a consumer's entity derives from Compze's closed generic
classes, so Compze's base classes have no way to put their own routes in a consumer's table. Step 3 sidestepped it for
`Tentity` by having the collection set the id. A removable entity also needs its removed tevent allowed to go unhandled,
which today comes from its base constructor's `TeventDispatcherConfig`.

**The design:** each of Compze's base classes has a protected static factory, typed on the class and its tevent root:

```csharp
abstract class JPNote : Taggregate<JPNote, INoteTevent, NoteTevent, INoteTevent<INoteTevent>, NoteTevent<NoteTevent>>, ITeventRouted
{
   static readonly TeventRouting<JPNote, INoteTevent> Appliers = DeclareAppliers(it => it          //Taggregate's protected static factory
     .For<INoteTevent.TagsChanged>(static (note, tevent) => note._tags = tevent.Tags));

   TeventRouting ITeventRouted.TeventAppliers => Appliers;
}
```

- **The factory puts the base class's own routes first,** then the class's:
  - `Taggregate` and `TeventiveComponent`: none;
  - `Tentity`: its created tevent sets its id, as the per-instance applier did. A consumer's entity then needs no applier
    of its own for the created tevent;
  - the removable entity: as `Tentity`, and its removed tevent may go unhandled;
  - `SharedTentity`: its created tevent sets its id.
- **A subclass's factory hides its base class's,** with `new`, and calls it with its own routes added. A consumer's class
  then reaches the most specific factory by name, as it reaches the nested shortcut classes.
- **`TeventRouting`'s own factories become internal.** That way a class's first table can only come from its base class,
  and can't skip the base class's routes. This is what the-design.md specified and step 3 didn't build. A subclass's
  table still comes from `Extend`.
- **The base classes' `ITeventRouted` still returns null, which now means that the class forgot its table.** The base
  constructor asserts that the class declares its appliers, and names the class, as it already asserts the exact class.
  An explicit implementation can't be abstract, so the assertion stands in for the compiler. Tevent handlers stay
  optional.

## 2. The table typed on its tevent root

`TeventRouting<TClass, TTevent>`, as the-design.md specified:

- `For`, `IgnoreUnhandled`, `Component` and `Entities` take only tevents of `TTevent`. A route to another hierarchy's
  tevent, which could never match, then doesn't compile, as it didn't with the dispatcher.
- `For` calls `TessageTypeInspector.AssertValidForSubscription`, as the dispatcher's subscriber does.
- `Extend<TSubclass>` keeps the root: `TeventRouting<VocabNote, INoteTevent>`.

## 3. Removable entities

- **Their collection creates, routes, then removes:** the removed tevent reaches the entity's appliers first, then the
  collection removes the entity. That is the order bugs-and-findings.md calls load-bearing, kept.
- The factory (1) lets the removed tevent go unhandled in the entity's table.

## 4. Shared tentities, and slots only of a class

- **Slots per instance go:** `ISharedTomponentSlot`, `SharedTomponentSlot` and the table entry that takes a slot object.
  Slots belong to the owner's class, as decided (item 24).
- **`SharedTomponentSlotOfAClass` asserts, when it is made, that its adopting wrapper implementation implements its
  adopting wrapper tevent.** That is the check the per-instance slot gained in 0.6.0-alpha against a wrapper that routes
  to another slot's tomponent. Its specification moves to a slot of a class. Without it, removing the per-instance slot
  would bring that bug back.
- **`SharedTentityCollection` is constructed with its slot in an owner** (`Slots.Checklist.In(this)`), as any shared
  tomponent in a slot of a class. It routes in code, as `Tentity`'s collection does: a created tevent adds a tentity, and
  every tevent reaches the tentity whose `EntityId` it carries. It is routing machinery with no appliers of its own, so it
  declares no table, and it is sealed: a subclass would have nothing to add.
- **`SharedTentity` declares its table** through its factory (1), and the collection dispatches to it through the table.

## 5. Tevent handlers on roots only

As the-design.md says, and as Compze's main branch has it: a component has never registered tevent handlers. Step 3's
prototype routed them into components. That goes: `ITeventRoutedMember.DispatchInternal` loses its flag, and components
their `TeventHandlers`.

## 6. The query models: how much they share with taggregates

Decided (decision 11): they move to the tables, and apply tevents and hold state as taggregates do. **How much of the
code the two share: option B, decided by Magnus, 2026-11-17,** on one condition: "If you can do it without client code
getting more complicated that is just great". He had hoped for it, and found the type constraints of these classes too
much of a jungle to redesign himself. The base classes' names are for the naming pass: "a taggregate is not a view
model", so they are named for applying tevents, not for query models. Magnus: "The only thing that taggregates add is
the ability to publish tevents."

With the tables, most of what each family has today goes: the dispatchers, the registrations, the parts subscribing to
their parents. What is left in each is small:

- **the root:** apply a tevent (the id from a created tevent, the version, dispatch), load from history, assert the
  invariants;
- **a component:** dispatch through its table, and the assertions at construction;
- **an entity:** its id, and its collection (create, route by id, remove).

**Option A, shared machinery in two families:** the query models use the same `TeventRouting`, the same entity collection
and the same collection routing. Their copies go: `QueryModelEntityCollection` is `EntityCollection` line for line, and
`IReadonlyQueryModelEntityCollection` has `IReadOnlyEntityCollection`'s members. The two families of classes stay
side by side, each thin. The duplicates left are the root's apply, a component's dispatch and an entity's id, about 40
lines in all. Taggregate's classes don't change.

**Option B, base classes that apply tevents and hold state, which both families derive from (decided):** the query
models and taggregates are siblings, and a taggregate is the one that also publishes:

```
TeventApplyingRoot<TSelf, TTevent>              apply, load from history, invariants   //review-naming: placeholders
 ├─ SelfGeneratingQueryModel<TQueryModel, TTevent>   + ApplyTevent and HandlesTevent, public
 └─ Taggregate<5>                                    + publishing, deletion
TeventApplyingComponent<TComponent, TTevent>    dispatch, the assertions
 ├─ the query models' Component
 └─ TeventiveComponent<6>                            + its parent, publishing
```

- **What it shares beyond option A:** the root's apply and the component's dispatch and safeguards, so a query model's
  table is checked exactly as a taggregate's is.
- **What it costs:** one more class in the hierarchy of every taggregate and taggregate component, with no more type
  parameters. The base classes show no public members of their own, and only Compze can derive from them.
- **What it doesn't share:** an entity's id field and created route, about 5 lines. `Tentity` derives from
  `TeventiveComponent`, so it can't also derive from an applying entity. The entity collection and its routing are
  shared, as in A.
- **Why:** it is Magnus's own model of the two, written into the types, and it shares all of the logic except an id
  field.
- **The condition, as built:** a consumer's taggregate, component and entity classes are declared exactly as before;
  only Compze's base classes gain a base class.

**Either way:**

- **A query model's part keeps no parent:** it publishes nothing, and it needed the parent only to register on it. It is
  constructed with its parent only for the assertion at construction, and keeps nothing of it. The query models' nested
  `NestedComponent`, `NestedEntity` and `RemovableNestedEntity` existed to constrain the tevent type. With tables typed
  on the tevent root (2), the parent's entry constrains it, so they go.
- **`HandlesTevent` answers from the table:** true when a route matches. It never caches, so it can't fall into the trap
  that 0.5.1-alpha fixed in the dispatcher: a dispatch of an unhandled tevent still throws after `HandlesTevent` said
  false. The AccountManagement sample uses it.
- **`SingleTaggregateQueryModelGenerator` stays on `IMutableTeventDispatcher`.** It is a service whose handlers fill a
  separate object, with a dispatcher built for each generation, not a class that applies tevents to its own state. It
  uses `ForGenericTevent`, which the tables don't have. That is run-time dispatch, which `IMutableTeventDispatcher`
  stays for.

## 7. The safeguards left from step 3

- **Matching shared with the dispatcher:** `TeventRoute.Matches` and `TeventRouting.MayGoUnhandled` use the dispatcher's
  code and its one translation rule, instead of a second implementation of them.
- **A part asserts, when constructed, that its parent's table routes its tevent type to a part,** once per pair of
  classes. A forgotten `.Component(...)` then fails at the first construction, instead of a note loading without the
  component's state. Still named, not solved: two components of one class with one entry forgotten.
- **A slot's route asserts that the shared tomponent it reaches was constructed with that slot.**

## 8. The public API that goes or changes, for the end review

- **Goes:**
  - from `Taggregate`, `TeventiveComponent`, `Tentity`, `SharedTomponent` and `SharedTentity`: `RegisterTeventAppliers()`
    and `RegisterTeventHandlers()`, and the constructors' `TeventDispatcherConfig` and `registerTeventAppliers`;
  - `ITeventiveInternals`: `RegisterTeventAppliersInternal`, `DeclaresTeventAppliersInternal` and `ApplyTeventInternal`.
    `PublishInternal` remains;
  - `ISharedTomponentInternals.ApplyTeventInternal`;
  - `ISharedTomponentSlot` and `SharedTomponentSlot`, the table entry that takes a slot object, and
    `SharedTentityCollection`'s constructor that takes one;
  - from the query models: the per-instance constructors, `RegisterTeventAppliers()`, `QueryModelEntityCollection`,
    `IReadonlyQueryModelEntityCollection`, `IQueryModelEntityCollectionManager`, and the nested-part classes.
- **Changes:** `TeventRouting<TClass, TTevent>`; its `DeclareAppliers` and `DeclareHandlers` internal, and a protected
  factory on each base class instead; `ITeventRoutedMember.DispatchInternal` without its flag; `ITeventRouted`'s null an
  error for appliers; `SharedTentityCollection` sealed.
- **Stays:** `IMutableTeventDispatcher`, `ITeventSubscriber` and `TeventDispatcherConfig`, for subscriptions made at run
  time; `ITeventRouted`, `ITeventRoutedMember`, `Extend`, `SharedTomponentSlotOfAClass`.

## 9. Specifications

- **New:**
  - an entity's id is set by its created tevent with no applier of the consumer's for it;
  - a removable entity's appliers have applied its removed tevent before it leaves its collection, and its table needs
    no applier for it;
  - a shared tentity in a slot of a class is created, publishes, and is routed by its `EntityId`, and a taggregate with
    such tentities round-trips through its snapshot;
  - a slot whose adopting wrapper implementation doesn't implement its adopting wrapper tevent is refused when it is made;
  - a class that declares no appliers is refused at its first construction, named;
  - a part whose parent's table has no entry for it is refused at its first construction, named;
  - a query model's `HandlesTevent` is false for an ignored tevent, and dispatching an unhandled tevent still throws
    after it was asked.
- **Moved to tables, and so kept as specifications of the same behaviour:** the composite taggregates and their query
  models (GuidId and IntegerId), the inheriting taggregate, the shared tomponents and tentities, `TaggregateTests` (its
  tevent handlers through `DeclareHandlers`), the tevent-refactoring test taggregate, the integration specifications'
  taggregates, and the AccountManagement sample. In all, 35 files of tests and the sample.

## 10. Increments

Each builds clean, passes the full suite, and is pushed.

1. **Matching shared with the dispatcher.** No public change.
2. **The factories and the table typed on its root,** with null an error for appliers, and every class that declares a
   table moved to them: Compze's, and then JAStudio's through taggregates-opus.
3. **Entities and removable entities on tables,** with their specifications.
4. **Shared tentities, slots only of a class, and the slot's check.**
5. **The query models,** in the option Magnus chooses.
6. **Registration per instance removed:** the remaining tests and the sample moved, the per-instance members deleted,
   tevent handlers in components removed, the parts' assertions of their parents' entries.
7. **The-design.md's "what is built" brought up to date.**

## Not in step 5

- **`ITaggregate.Commit`'s callback and the public `TeventStream`,** which decision 10 removes for a taggregate in a store.
  The composite specification feeds its query model through them. That is a change of its own.
- **The rewrite of the published `reusable-components.md`,** which waits for the naming pass.
- **`BeforeHandlers`, `AfterHandlers` and `ForGenericTevent` on the tables,** which are on Magnus's list in decision 10.
