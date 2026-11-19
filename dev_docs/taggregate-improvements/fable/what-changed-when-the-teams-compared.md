# What changed when the two teams compared

2026-11-16. The four sessions, jastudio-rep-fable and taggregates-fable of this team and jastudio-rep-opus and
taggregates-opus of the other, compared their conclusions and settled the differences. The result is in
[../shared](../shared/README.md). This lists where it differs from the other documents in this folder, which are left as
they were.

## The headline

**The two teams had reached the same three changes separately, and differed in details.** Of those, this team's queue,
its slot as a static of the owner's class, and its account of what an exception leaves behind went into the shared
design. The other team's way for an instance to reach its class's table, its prototype in Compze's real base classes,
and, on JAStudio's own reasons, its snapshot marker did too.

## Where the shared design replaces what this folder says

| This folder | The shared design | Why |
|---|---|---|
| The class's declaration is handed through the base constructor, and the instance keeps a reference to it | A protected virtual property returns the class's static table, as `WrapperTeventImplementation` does | Nothing is kept in an instance, where the reference cost 8 bytes in every teventive, and no constructor parameter passes up an inheritance chain |
| Forgetting it can't happen, since the constructor requires it | The table asserts, when it routes, that the instance's class is exactly the class it was declared for | A subclass that forgets its table, or adds an applier for a tevent its base class already applies, fails at its first tevent instead of losing the applier silently |
| "Compacted-history", a term coined here | "Snapshot" | The standard word, and it fits |
| The first tevent of the list is a creation tevent | It carries a snapshot marker of its own | JAStudio's reasons: a note is created in several ways, each creation tevent saying what that creation set; and only a snapshot tevent must be complete, so only it can be checked against every property-updated interface of its class |
| The tevents are numbered 1 to k, as `Publish` would | The whole snapshot is version 1, with no version parameter | With the marker a snapshot isn't a history that starts with a creation. `Version` after a load is then 1 whatever the note's shape |
| A component holds its parent as what it publishes through | A component is typed on the class that declares it | The other team found that today's nested `Component<…>` gives a subclass's component the root class as its parent |
| A loaded tevent needs no wrapper object, as part of the design | Left out of the first version, and measured later | It is a second path through every kind of route, and saves garbage at a start, not kept bytes |

## What this team brought that the shared design took

- **The queue** that hands published tevents over in the order published, with the hook never inside itself. The other
  team's prototype first handed a publish from inside the hook over at once, which gave A, C, B for tevents published
  A, B, C. It ran this team's case as a specification on its own branch, saw it fail, and built the queue.
- **The slot as a static of the owner's class,** making the adopted tevent for publishing and for a snapshot.
- **What an exception leaves behind,** and that a taggregate could refuse later use: now prototyped on the other team's
  combined branch, for Magnus to decide.
- **The stack overflow** as the worse form of the bug in `Publish`, found by taggregates-fable.
- **The two bugs first,** and `ITaggregateTevent` as a plain `ITevent` as independent of the rest, with what it
  touches searched.
- **The eight behaviours** that JAStudio's notes rely on and no specification pins, from taggregates-fable.

## What was built after the teams compared

**The slot as a static of its owner's class, in Compze's real base classes,** on the other team's combined prototype:
branch `taggregate-experiments-fable-static-slots`, commit `fb75d60a0`, in
`C:\Dev\Compze.worktrees\taggregate-experiments-fable-static-slots`. Sixteen specifications, with a slot in the
taggregate itself, two slots of one class in a component, and a snapshot whose adopted tevents the slots made.
Compze.Tests.Unit passes, 496 of 496. The other test assemblies were not run on that branch.

80,000 vocabs, each a root, four components and four shared tomponents in one of the components, loaded from a
snapshot of five tevents:

| | Kept | Allocated | Time |
|---|---|---|---|
| Plain objects | 350 B | 352 B | 0.15 µs |
| Registered per instance | 9,952 B | 22,104 B | 36.6 µs |
| Declared per class, a slot object for each slot of each instance | 832 B | 4,192 B | 4.4 µs |
| Declared per class, slots that belong to the class | 640 B | 4,800 B | 4.8 µs |

- **A slot that belongs to the class saves 48 bytes for each slot,** about 26 MB for JAStudio's notes.
- **About 120 of the 640 bytes are the prototype's scaffolding** for the per-instance style, so a final design keeps
  about 520 bytes for this vocab. The standalone prototype in [what-was-run-and-measured.md](what-was-run-and-measured.md)
  measured 584 bytes with a reference to the declaration in each teventive, which is 512 without: the two agree.
- **Two findings from the allocated column:** loading on the combined prototype allocates about twice what the
  standalone prototype does, from closures in assertions and in `ScopedChange`; and `PublisherTevent.WrapIn` allocates a
  closure at every call, also when the constructor is cached, which is in `main` today.
- **What the real generics forced:** a slot's owner can only be constrained to a class, so the slot asserts when it is
  made that its owner publishes the owner's tevents; and a slot's declaration has five type arguments, which a slot
  type nested in the base class could shorten.

## Who reports

jastudio-rep-opus reports to Magnus for Compze and keeps the shared folder. taggregates-fable reports for JAStudio.
