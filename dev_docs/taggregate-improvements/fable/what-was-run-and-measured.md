# What was run and measured

2026-11-16. The experiments behind the other documents in this folder.

## The headline

**A prototype of tevent routing decided once for each class keeps 584 bytes for a vocab that today's Taggregate keeps
10,204 bytes for, and loads it in under 3 µs instead of 35.** The same data as plain objects is 350 bytes. Every claim
the other documents made from reading the code held when run, and one flaw is worse than read: an observer of the
tevent stream that publishes on its taggregate can end the process with a stack overflow.

## Where the experiments are

On the branch `taggregate-experiments-fable`, in the worktree `C:\Dev\Compze.worktrees\taggregate-experiments-fable`,
under `experiments/taggregate-improvements-fable`. Each program is built by itself against Compze.Teventive 0.6.0-alpha
from nuget.org. Nothing in Compze is changed.

| Program | What it is |
|---|---|
| `TaggregateCost` | The JAStudio session's measurement program, as it wrote it. Run again here, it gives its numbers: 350, 1,632 and 5,629 bytes kept. |
| `ClaimsCheckedByRunning` | Runs what these documents claimed from reading the code. |
| `TeventRoutingPerClassPrototype` | The prototype, the same vocab written against today's Taggregate and against the prototype, and a program that measures the two and runs the prototype's behaviour. |

## What a vocab costs: today's Taggregate and the prototype

80,000 vocabs loaded from their stored state, as JAStudio would load them. Both publish and apply the same tevents, so
only the routing differs. The stored arrays and strings are shared with the stored state, and so are in no column.

| | Kept | Allocated | Time |
|---|---|---|---|
| Plain objects: a root, four parts and four sets of words | 350 B | 352 B | 0.2 µs |
| Today's Taggregate, the root alone, with four tevent appliers | 1,632 B | 2,840 B | 3.6 to 3.9 µs |
| The prototype, the same | 168 B | 608 B | 0.7 µs |
| Today's Taggregate, with four components of two tevent appliers each | 5,526 B | 8,710 B | 15 to 17 µs |
| The prototype, the same | 392 B | 648 B | 0.7 µs |
| Today's Taggregate, with the four components and four shared tomponents in one of them | 10,204 B | 19,648 B | 34 to 40 µs |
| The prototype, the same | 584 B | 2,040 B | 2.6 to 2.8 µs |

The bytes were the same in three runs. The times are the range of the three, with the machine at 10 to 13 % load from
other work before each.

**For each teventive,** from the differences between the rows:

| | Today | The prototype | Plain object |
|---|---|---|---|
| A root | 1,632 B | 168 B | 96 B |
| A component | 974 B | 56 B | 40 B |
| A shared tomponent, in its slot | 1,170 B | 48 B | 24 B |

What the prototype keeps beyond the plain object: in a component its parent and its class's declaration, in a shared
tomponent its owner, its slot and its class's declaration, and in a root its two declarations, its id as an object, its
version, and the fields `Publish` uses.

**Publishing,** on each of the 80,000 vocabs of the last shape. Kept is what remains after `Commit` has taken the
tevents:

| | Kept | Allocated | Time |
|---|---|---|---|
| Today: a change of the answer, the first tevent of its type on the instance | 416 B | 1,320 B | 1.5 to 1.6 µs |
| Today: a second change of the answer | 0 B | 376 B | 0.8 to 1.1 µs |
| Today: a set of words changed, through its slot in a component | 40 B | 856 B | 1.6 to 2.5 µs |
| The prototype: a change of the answer, the first of its type | 0 B | 320 B | 0.4 µs |
| The prototype: a second change of the answer | 0 B | 320 B | 0.3 to 0.4 µs |
| The prototype: a set of words changed, through its slot in a component | 40 B | 440 B | 0.4 to 0.5 µs |

- **Today an instance keeps about 400 bytes more for every tevent type it meets:** the first row. Its dispatchers
  resolve each type for themselves and keep the result. The 40 bytes in the rows of the set of words are the new array
  of words.
- **Of the prototype's 320 bytes, 176 are the two lists,** which it creates for a publish and lets go when they are
  empty. Keeping one tevent in a field, and a list only from the second, would take most of that away.
- **A `TessageId`,** which every tevent of a taggregate makes today, is 32 bytes and 0.08 µs.

**The JAStudio session built a prototype of its own,** about 150 lines with the same shape, and measured it on the two
models it had measured 0.6.0-alpha with. Its numbers agree with these: a root with four tevent appliers 119 bytes
against 1,632; with four components 472 against 5,629, where its plain objects, arrays included, are 350; a shared
tomponent 56 bytes against 552, and 971 after its first tevent; loading about fifteen times faster. It puts JAStudio's
notes at about 26 MB over plain objects. Its prototype makes no wrapper when it loads, which
[a-taggregate-without-a-tevent-store.md](a-taggregate-without-a-tevent-store.md) takes up: an adopted tevent then
loads in 232 bytes, where this prototype, which wraps, allocates about 350.

## What it comes to for JAStudio

An estimate, from the bytes above and the JAStudio session's count of its notes' parts (a vocab about 12 components and
10 shared tomponents, a sentence about 3 and 5, a kanji about 5 and 6):

- **Memory:** as the prototype, its 79,720 notes keep about 30 MB more than plain objects would. As today's
  Taggregate, the JAStudio session's own estimate is about 1.1 GB.
- **Loading,** which is how JAStudio starts, with about 570,000 tevents: not measured for its notes. By the rows
  above, the vocab of five tevents loads 13 to 15 times faster on the prototype, and allocates a tenth.

## The claims, run

`ClaimsCheckedByRunning`, against Compze.Teventive 0.6.0-alpha. Every one came out as claimed.

| Claim | Observed |
|---|---|
| An observer of the tevent stream that publishes once on its taggregate | Notified of `TextChanged("b")`, `TextChanged("b")` again, `TextChanged("c")`, then `Publish` throws `InvalidOperationException`: "Collection was modified". The state has changed: the text is "c", the version 3. |
| An observer that publishes every time it is notified of the tevent it reacts to | The process ends with exit code 0xC00000FD, a stack overflow, which nothing can catch. Each inner `Publish` notifies it of the same tevent again. Found by the JAStudio session. |
| A tevent published from inside `Commit`'s callback, while the callback enumerates | The enumeration throws "Collection was modified". |
| The same, after the callback has copied the tevents | The tevent is applied, the version is 2, and no call to `Commit` ever hands it over. |
| A tevent handler that throws | The tevent stays applied and uncommitted, and nothing is notified. The next publish notifies and commits both. |
| A tevent constructed outside `Publish`, loaded with `LoadFromHistory` | The taggregate is at version 0, and its next tevent is refused: "The first published tevent … did not implement ITaggregateCreatedTevent". |
| A slot in a component, which no specification covers | Publishing through it works, and so does loading from the committed history. |
| `For<an owner's interface that one slot's adopting wrapper tevent implements>` | Receives what is adopted through that slot, as the adopting wrapper tevent, and nothing from the other slot. |
| `For<the shared tomponent's own tevent type>` | Receives nothing of what is adopted. |
| A generic, covariant tevent interface that isn't a wrapper | Matched by covariance for a class. For a struct it matches nothing, without an error. |

The JAStudio session ran its own checks of the same claims, with the same results, and of eight behaviours its notes
rely on, which all hold: see
[the-dialog-with-the-jastudio-session.md](the-dialog-with-the-jastudio-session.md).

## The prototype's behaviour, run

What [a-taggregate-without-a-tevent-store.md](a-taggregate-without-a-tevent-store.md) proposes, as the prototype does
it:

| Proposed | Observed |
|---|---|
| A publish from inside the hook is handed over when the call in progress returns, in the order published | Two calls of the hook, `Renamed("b") v2` and then `Renamed("c") v3`. The hook was never inside itself. |
| A tevent handler's tevent is handed over with the tevent it reacted to | One call of the hook, with both. This is JAStudio's sentence, reparsed when its configuration changes. |
| Committing from the hook, with a subscriber that publishes on the same note from inside the commit | Committed `Renamed("b") v2`, then `Renamed("c") v3`. Nothing left uncommitted, nothing lost. |
| A hook that throws | The exception leaves `Publish`. The next publish is handed over as usual, and `Commit` then hands over both tevents. |
| A second thread calling `Publish` while another is in it | Refused: "Thread 2 called Publish on a ReactingNote while thread 4 was in it." |
| Many threads at once, each loading and publishing on vocabs of its own | 20,000 of 20,000 vocabs have the state their own thread gave them, through the declarations all the threads share. |
| A slot declared below the declaration that enters it, and a lambda that captures | Each is refused where the class is first used, with a message that says what to change. |

## What writing the prototype showed about the declaration

- **A component's entry needs no type argument:** `.Component(vocab => vocab.Forms)`. C# infers the root interface of
  the component's tevents from what the lambda returns, when the entry takes a lambda that returns an interface the
  component's base class implements with that type argument.
- **A slot's entry needs none either:** `.SharedTomponent(SeeAlsoSlot)`.
- **A slot's own declaration is long,** with four type arguments in the field's type and two in the call:

  ```csharp
  static readonly Slot<SynonymsPart, VocabTevent.Synonyms, IWordSetTevent, ISeeAlsoTevent<IWordSetTevent>> SeeAlsoSlot =
     DeclareSlot<IWordSetTevent, ISeeAlsoTevent<IWordSetTevent>>(typeof(SeeAlsoTevent<>), synonyms => synonyms.SeeAlso);
  ```

  A slot type nested in the base class knows the owner's two, and the call can be a `new`:

  ```csharp
  static readonly Slot<IWordSetTevent, ISeeAlsoTevent<IWordSetTevent>> SeeAlsoSlot = new(typeof(SeeAlsoTevent<>), synonyms => synonyms.SeeAlso);
  ```

- **The compiler warns of a slot declared below the declaration that enters it,** when nullable reference types are
  on, which they are in Compze and in JAStudio (CS8604). The assertion is then the second line of defence.
- **A component with a primary constructor reads well:**
  `class FormsPart(Vocab vocab) : Component<FormsPart, IVocabTevent.Forms, VocabTevent.Forms>(vocab, Appliers)`, with
  the declaration as the first thing in its body.
- **A static field can't be `var`,** so a declaration's type is written out at each one. Nested in the base class, as
  `TeventAppliers`, it is one word.
- **The prototype's taggregate has three type parameters,** and is given its wrapper tevent implementation as
  `typeof(VocabTevent<>)` in the declaration. Compze's has five, the wrapper's interface and implementation among
  them, which type the tevent stream and would type the hook. Which the real one should have is a question for its
  design.

## What the prototype is not

It was written to be measured, and to try the declaration's shape in real C#. It has no entities and collections, no
`ForWrapped`, `BeforeHandlers`, `AfterHandlers` or `IgnoreUnhandled<T>`, no tevent stream, no `TaggregateTypeValidator`,
and a raw lock where a class resolves a tevent type for the first time.

## What was not run

- **`ITaggregateTevent` as a plain `ITevent`:** nothing of it. What it changes is from reading the code.
- **JAStudio's own note types** on the prototype.
- **Compze's suite:** nothing in Compze was changed, so there was nothing to run it against.
