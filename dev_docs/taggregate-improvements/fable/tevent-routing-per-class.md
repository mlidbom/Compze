# Tevent routing decided once for each teventive class

A proposal, 2026-11-16. Nothing is built. The names in the code below are placeholders for Magnus's pass over the names.
What it replaces, and what that costs, is in [what-a-taggregate-costs-today.md](what-a-taggregate-costs-today.md).

## The proposal

**Each teventive class declares its tevent appliers once, in a static declaration, and every instance of the class
uses it.** A teventive is a taggregate, a component, an entity or a shared tomponent. The declaration has the verbs of
today's registration, and the same rule: all matching tevent appliers are called, in the order declared. Two things
differ:

- **A tevent applier takes the instance,** since it is no longer a closure over one.
- **A child is an entry in the declaration,** by a lambda that gets it from the instance: a component, a collection of
  entities, a shared tomponent. It is not something that subscribes to its parent when it is constructed.

```csharp
class Account : Taggregate<Account, IAccountTevent, AccountTevent, IAccountTevent<IAccountTevent>, AccountTevent<AccountTevent>>
{
   static readonly TeventAppliers Appliers = DeclareTeventAppliers()
     .For<IAccountTevent.PropertyUpdated.Email>((account, tevent) => account.Email = tevent.Email)
     .For<IAccountTevent.PropertyUpdated.Password>((account, tevent) => account.Password = tevent.Password)
     .IgnoreUnhandled<IAccountTevent.LoggedIn, IAccountTevent.LoginFailed>();

   Account() : base(Appliers) {}
```

With children, in the order the tevent is to reach them:

```csharp
class Root : Taggregate<Root, IRootTevent, RootTevent, IRootTevent<IRootTevent>, RootTevent<RootTevent>>
{
   static readonly TeventAppliers Appliers = DeclareTeventAppliers()
     .Component(root => root.Component)
     .Entities(root => root._entities)
     .For<IRootTevent.PropertyUpdated.Name>((root, tevent) => root.Name = tevent.Name);

   public Root(string name) : base(Appliers)
   {
      Component = new Component(this);
      _entities = RemovableEntity.CreateSelfManagingCollection(this);
      Publish(new RootTevent.Created(new TaggregateId(), name));
   }
```

Tevent handlers, which may publish, are declared the same way, with `DeclareTeventHandlers()`.

**What it costs the reader:** `(account, tevent) => account.Email = tevent.Email` where today's code says
`e => Email = e.Email`, and a static field and a constructor argument in each class.

## How it works

- **`DeclareTeventAppliers()` is a protected static method of the base class,** so the declaration is typed by the
  class's own type arguments: `For<THandledTevent>` compiles only for a tevent of the class's hierarchy, as today, and
  the lambda's first parameter is the class.
- **The instance is handed its class's declaration through the base constructor.** That parameter replaces
  `TeventDispatcherConfig? teventAppliersDispatcherConfig`, which is the same thing done by halves: routing policy of
  the class, handed through the instance's constructor. A static field is initialized once, by the runtime, before the
  first instance exists, so nothing has to be made lazy or locked.
- **Resolving stays what it is, done once for each class.** The first time a class meets a wrapper tevent type, the
  entries that match are resolved into an array, and published in a dictionary that is never changed afterwards and is
  read without a lock: the technique `Subscriptions` uses today. A tevent that no entry matches throws
  `TeventUnhandledException` unless the declaration lets it go unhandled, as today.
- **Applying a tevent** is then one dictionary lookup for each teventive the tevent reaches, and a call for each entry
  with the instance as argument.
- **What an instance keeps:** one reference to its class's declaration. No dispatcher, no subscriptions, no closures.
  A component still holds its parent, to publish through. A collection still holds its entities. A slot still holds its
  shared tomponent.
- **Several threads:** the declarations are immutable, and the resolved handlers of a class are published by replacing
  an immutable dictionary. JAStudio's full rebuild, which publishes from many threads at once, each on notes of its
  own, meets a lock only the first time a class sees a tevent type.

## What each kind of entry does

| Entry | Matches | Does |
|---|---|---|
| `For<T>`, `ForGenericTevent<T>` | every wrapping of a tevent assignable to `T` | calls the lambda with the instance and the tevent |
| `ForWrapped<T>`, `ForWrappedGeneric<T>` | a wrapper assignable to `T` | calls the lambda with the instance and the wrapper |
| `BeforeHandlers<T>`, `AfterHandlers<T>` | as `For<T>`, when any other entry matches | as today |
| `Component(accessor)` | the component's root tevent interface, as its `For<TComponentTevent>` on the parent does today | hands the tevent to the component, which routes it by its own class's declaration |
| `Entities(accessor)` | the entity's root tevent interface | creates and adds the entity on its creation tevent, hands the tevent to the entity with its id, and removes a removable entity on its removal tevent, in that order |
| `SharedTomponent(slot)` | the slot's adopting wrapper tevent, in a root or in a component | opens it and hands the tevent inside to the shared tomponent |
| `IgnoreUnhandled<T>`, `IgnoreAllUnhandled` | | lets such a tevent go without a tevent applier |

A component is handed the tevent without its wrapper, as today. Its declaration can be resolved by the tevent's own
type, so the tevent need not be wrapped again at each component level, which it is today.

## Shared tomponents and their slots

In JAStudio the same class of part several times in one note is the normal case: a vocab has six sets of words, three
of them of one class, and four lists of media references, so about ten slots. A shared tomponent must then cost an
instance as little as a component does. Today it costs as much as a component, and a `SharedTomponentSlot` object more.

- **A slot becomes a static of the owner's class:** the adopting wrapper tevent, and the lambda that gets the shared
  tomponent from the owner. It is declared once, entered in the owner's declaration, and handed to the shared tomponent
  when the owner constructs it.
- **A shared tomponent keeps two references:** its owner, to publish through, and its slot. There is no slot object for
  each instance, and nothing registered on the owner.
- **The slot stays the one place that wraps** a tevent in its adopting wrapper tevent: when the shared tomponent
  publishes, and when the owner loads (see
  [a-taggregate-without-a-tevent-store.md](a-taggregate-without-a-tevent-store.md)).
- **Each slot still has an adopting wrapper tevent of its own,** an interface and a class. That type is what tells two
  shared tomponents of one class apart, for routing and for subscribers.
- **Handing a slot to a shared tomponent allocates nothing:** `In(this)` below is a struct of the owner and the slot.
- **A slot in a component is the same as a slot in a root.** It is JAStudio's usual case: the sets of words are in
  the vocab's related vocab, the lists of media references in each note's source data.
- **A slot must be declared above the declaration that enters it.** Static fields are initialized in the order
  written, and across the files of a partial class in no order that C# promises, so a slot declared below is null
  where it is entered. The declaration asserts it, with a message that says what to move.
- **A shared tomponent's class declares its tevent appliers as any teventive does.** A subclass's declaration starts
  from its base class's, and a generic class has a declaration for each closed type, since that is what a static
  field of a generic class is. JAStudio has both: `MutualWordRelationship` under `WordRelationship`, and
  `AudioReferenceList` under `MediaReferenceList<AudioReference>`. `SharedTomponent<TTomponentTevent>` has no type
  parameter for the class itself, so its declaration names the class: `DeclareTeventAppliers<PostalAddress>()`.

```csharp
static readonly Slot<IPostalAddressTevent> ShippingAddressSlot =
   DeclareSlot<IPostalAddressTevent, IShippingAddressTevent<IPostalAddressTevent>>(typeof(ShippingAddressTevent<>), order => order.ShippingAddress);

static readonly TeventAppliers Appliers = DeclareTeventAppliers()
  .For<IOrderTevent.Created>((_, _) => {})
  .SharedTomponent(ShippingAddressSlot)
  .SharedTomponent(BillingAddressSlot);

OrderTaggregate() : base(Appliers)
{
   ShippingAddress = new PostalAddress(ShippingAddressSlot.In(this));
   BillingAddress = new PostalAddress(BillingAddressSlot.In(this));
}
```

## Base classes and subclasses

A base class's tevent appliers come before a subclass's today, because its constructor runs first. In a declaration
that is said outright: a subclass's declaration starts from its base class's.

```csharp
class AnimalTaggregate : Taggregate<AnimalTaggregate, IAnimalTevent, AnimalTevent, IAnimalTevent<IAnimalTevent>, AnimalTevent<AnimalTevent>>
{
   protected static readonly TeventAppliers AnimalAppliers = DeclareTeventAppliers()
     .For<IAnimalTevent.Born>((animal, tevent) => animal.TimeOfBirth = tevent.UtcTimeStamp);

   protected AnimalTaggregate(TeventAppliers appliers) : base(appliers) {}
}

class CatTaggregate : AnimalTaggregate
{
   static readonly TeventAppliers Appliers = AnimalAppliers.ThenFor<CatTaggregate>()
     .For<ICatTevent.Purred>((cat, tevent) => cat.Purrs++);

   CatTaggregate() : base(Appliers) {}
}
```

Compze's own base classes do the same for what they register today: `Tentity` puts the applier that sets the entity's
id first in every entity's declaration.

## What goes, and what stays

- **Goes:** `RegisterTeventAppliers()` and `RegisterTeventHandlers()` on the base classes, the
  `TeventDispatcherConfig` constructor parameters, the `registerTeventAppliers` flag, and
  `ITeventiveInternals.RegisterTeventAppliersInternal`, through which children subscribe to their parents.
- **Stays, unchanged:** `IMutableTeventDispatcher`, `ITeventSubscriber` and `TeventDispatcherConfig`, for code that
  subscribes to tevents on a dispatcher of its own. JAStudio's one dispatcher for its whole collection is that.
- **Is rewritten:** every taggregate, component and entity in the tests and the sample, which is the 56 registrations
  in 41 files, and the taggregate examples in `_docs` and the README. Specifications that assert what a taggregate does
  through its public members stay as they are.
- **The query models** (`SelfGeneratingQueryModel` and its families) should use the same declarations, instead of a
  second implementation of the tree with dispatchers of their own.

It changes protected members of public base classes, so it is a change to Compze's public API: Magnus's to decide.

## Why not the alternatives

- **`Apply(ISomeTevent)` methods, bound once for each class by an attribute.** They would be bound in the order
  reflection lists a class's methods, which .NET doesn't promise to be the order in the source. The rule "in the
  order declared" would have to become a rule derived from the tevent types, and two interfaces that one tevent
  implements and that don't inherit each other have no order. A child has no place in it. What `For` and `ForWrapped`
  check when compiling would be checked when running.
- **A source generator** could bind such methods in the order of the source. Compze has no generator of any kind
  today, and the declaration needs none.
- **Keeping the registration in the constructor, and binding the first instance's lambdas to every later instance.**
  The code would stay as it is and read as registration for the instance, while behaving as registration for the
  class. A delegate would still be allocated for each tevent applier of each instance.
- **A list of child receivers in each instance,** which a parent asks in turn after its own tevent appliers (the
  JAStudio session's first shape). It costs a list for each teventive with children, and fixes the order: own first,
  then children. An entry by a lambda costs nothing for an instance, and keeps the order something the class states.

## What JAStudio needs of it

From the JAStudio session's answers (see
[the-dialog-with-the-jastudio-session.md](the-dialog-with-the-jastudio-session.md)):

- **Shared tomponents in every note type:** about 10 in a vocab beside its 12 components, 4 to 6 in a sentence beside
  3, and 6 in a kanji beside 5. Most of them sit in a component. So the slots are on its hottest path. It has no
  entities.
- **Base classes with tevent appliers:** `JPNote`, with the note's tags, under the vocab, sentence and kanji notes, each
  with a wrapper of its own, as in the `AnimalTaggregate` specifications.
- **Tevent handlers in a root, and none in components.** A change to a sentence's configuration has the sentence
  reparsed, and the new parsing result published: a tevent handler in `SentenceNote`. Where one part of a note follows
  another, it is one tevent that both apply. Only a root has tevent handlers today, and that can stay.
- **The order across a teventive and its children only on principle:** nothing in its notes depends on it. What it
  relies on is that every tevent applier in the tree runs before any tevent handler.

## Open

- **The declaration through the constructor, or found by the base class.** The alternative to the constructor
  argument is a protected virtual method that the base class calls once for each class, on its first instance. It
  saves the argument, and brings a virtual call from a constructor and a lambda that can capture that first instance by
  mistake. I'd take the argument.
- **A lambda in a declaration must capture nothing,** since every instance shares it. `static` lambdas make the
  compiler check it; the declaration can assert it for the rest.
