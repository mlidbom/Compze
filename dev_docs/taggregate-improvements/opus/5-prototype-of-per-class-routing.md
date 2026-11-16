# The prototype of per-class tevent routing: what it measured, and what it showed

Built and measured on 2026-11-16 by jastudio-rep-opus. An experiment, not for merging: branch
`taggregate-experiments-opus`, commit `8349b7389`, in the worktree `C:\Dev\Compze.worktrees\taggregate-experiments-opus`.
It prototypes [tevent routing decided once per class](2-tevent-routing-decided-once-per-class.md).

## Conclusions

taggregates-opus then merged this prototype with its prototype of [loading from a snapshot](1-loading-a-taggregate-from-a-snapshot.md)
and [the per-cascade hook](3-told-once-per-publish-cascade.md), and measured the combination: see
[all three combined](#all-three-changes-combined), below.

- **The memory problem goes away.** A loaded note with 15 components retains 1,168 B instead of 11,256 B, against 733 B
  as plain objects. About 145 B of that is the prototype's own scaffolding, below. An edit to each component then adds
  216 B instead of 6,488 B.
- **Loading is about 40 times faster:** 80,000 notes of 15 components load in 0.07 to 0.09 s instead of 2.9 to 3.1 s,
  on one thread.
- **The API sketch holds up in real code.** Inheritance, a component typed on the subclass that declares it, a
  component's own tevent handlers, and the check that handlers hold no state all work, and are specified.
- **Every existing specification passes unchanged:** 403 in `Compze.Tests.Unit`'s CQRS namespace, plus the prototype's 12.

## What the prototype covers

- **`TeventRouting<TInstance>`** in Compze.Teventive: the per-class table, with `DeclareAppliers`, `DeclareHandlers`,
  `Extend<TSubclass>`, and a `Declaration` offering `For<TTevent>`, `Component(…)` and `IgnoreUnhandled<TTevent>()`. It
  resolves the routes for each wrapper tevent type once per class, in a `ConcurrentDictionary`.
- **`Taggregate` and `TeventiveComponent`** use a class's routing when the class declares one, through the virtual
  properties `TeventAppliers` and `TeventHandlers`. Otherwise they fall back to their per-instance dispatchers, which
  are now created only when first registered on. That fallback is what keeps every existing specification running
  unchanged, and it's scaffolding: the final design has no per-instance dispatchers.
- **Components get tevent handlers,** composed through the parent's `.Component(…)`, which is new.

Not covered: entity collections, shared-tomponent slots, `SelfGeneratingQueryModel`, sharing the matching rules with
`IMutableTeventDispatcher`, and composing the routes into nested components once per type. Each component level still
looks its routes up in its own class's table, one dictionary lookup per level.

## The API, as written in the prototype's specification

```csharp
class Note : Taggregate<Note, INoteTevent, NoteTevent, INoteTevent<INoteTevent>, NoteTevent<NoteTevent>>
{
   protected static readonly TeventRouting<Note> NoteAppliers = TeventRouting<Note>.DeclareAppliers(it => it
     .For<INoteTevent.Created>(static (_, _) => {})
     .For<INoteTevent.Renamed>(static (note, tevent) => note.Name = tevent.Name));

   protected override TeventRouting TeventAppliers => NoteAppliers;
   ...
}

class VocabNote : Note
{
   static readonly TeventRouting<VocabNote> VocabNoteAppliers = NoteAppliers.Extend<VocabNote>(it => it
     .Component(static vocab => vocab.Forms));

   static readonly TeventRouting<VocabNote> VocabNoteHandlers = TeventRouting<VocabNote>.DeclareHandlers(it => it
     .Component(static vocab => vocab.Forms));

   protected override TeventRouting TeventAppliers => VocabNoteAppliers;
   protected override TeventRouting TeventHandlers => VocabNoteHandlers;
   ...
}

class FormsComponent : TeventiveComponent<VocabNote, INoteTevent, NoteTevent, FormsComponent, IVocabTevent.Forms, VocabTevent.Forms>
{
   static readonly TeventRouting<FormsComponent> FormsAppliers = TeventRouting<FormsComponent>.DeclareAppliers(it => it
     .For<IVocabTevent.Forms.Updated>(static (forms, tevent) =>
      {
         forms.All = tevent.AllForms.Split(',');
         forms.Summary = $"{forms._vocab.Question} {tevent.AllForms}";
      })
     .For<IVocabTevent.Forms.Counted>(static (forms, tevent) => forms.Count = tevent.Count));

   static readonly TeventRouting<FormsComponent> FormsHandlers = TeventRouting<FormsComponent>.DeclareHandlers(it => it
     .For<IVocabTevent.Forms.Updated>(static (forms, _) => forms.Publish(new VocabTevent.Forms.Counted(forms.All.Count))));

   protected override TeventRouting TeventAppliers => FormsAppliers;
   protected override TeventRouting TeventHandlers => FormsHandlers;
   ...
}
```

What writing it showed:

- **`.Component(static vocab => vocab.Forms)` infers the component's tevent type,** from the `ITeventRouted<TTevent>`
  that the component implements. No type argument is needed.
- **A static lambda reaches private members** of the instance it's given (`forms._vocab`, private setters), and can call
  the protected `Publish`, because it's declared inside the class.
- **Each class repeats one line per table,** `protected override TeventRouting TeventAppliers => …;`. That's the one
  piece of boilerplate. It follows the existing `WrapperTeventImplementation` property.
- **Mixing the two styles fails loudly:** a class that declares a routing and also calls `RegisterTeventAppliers()`
  throws, and so does a component registered per instance under a parent that declares its routing.

## Measured

`benchmark/generate_both.py` generates one program with the same note in both styles: registered per instance, as
today, and declared per class. It's built against the prototype's `Compze.Teventive`. Each component has 3 string
properties with one applier each, and the note is loaded through `LoadFromHistory` from one creation tevent. Bytes per
note, over 10,000 notes, measured on .NET 11 with non-concurrent workstation garbage collection:

| Components | Style | Retained, loaded | Allocated by a load | Retained more after an edit of each component | Allocated by those edits |
|---:|---|---:|---:|---:|---:|
| 15 | plain objects | 733 | | | |
| 15 | registered per instance | 11,256 | 19,304 | +6,488 | 25,376 |
| 15 | **declared per class** | **1,168** | **1,872** | **+216** | **10,312** |
| 6 | plain objects | 303 | | | |
| 6 | registered per instance | 5,000 | 8,784 | +2,616 | 9,336 |
| 6 | **declared per class** | **592** | **1,296** | **+152** | **4,184** |
| 1 | plain objects | 63 | | | |
| 1 | registered per instance | 1,528 | 2,936 | +512 | 1,560 |
| 1 | **declared per class** | **272** | **976** | **+120** | **776** |

For JAStudio's 80,000 notes at 15 components, that's about 93 MB loaded instead of about 900 MB, against 59 MB as plain
objects.

The registered style measures slightly less than taggregates-opus measured with 0.6.0-alpha (11,457 B and +7,094 B).
That's because the prototype creates the handlers' dispatcher only when a handler is registered.

**Loading 80,000 notes of 15 components, on one thread,** three rounds each: 3.12, 2.88 and 3.03 s registered per instance;
0.09, 0.07 and 0.07 s declared per class. The machine's processor load averaged 11% over the run, sampled each second.
The benchmark's one thread is about 3% of the machine's 32 logical processors, so other work took about 8%: some
disturbance, far too little to explain a factor of 40.

### What the prototype's scaffolding costs, and what's left

The fallback to per-instance dispatchers costs, in a class that declares its routing, 24 B on the taggregate and 8 B on
each component. For 15 components that's 144 B, so the final design should retain about 1,024 B per note: about 290 B
over plain objects. What remains is the taggregate's `TaggregateId`, its two lists of tevents and `TeventStream`'s
observable (about 150 B), and the parent reference on each component (8 B each).

Making the two lists and `TeventStream` lazy would take most of the remaining 150 B. That's taggregates-opus's part,
for `TeventStream`, under [told once per publish cascade](3-told-once-per-publish-cascade.md).

### The edits still allocate

The 10,312 B allocated by 15 edits is about 690 B per publish: the tevent with its `TessageId` and timestamp, the
wrappers, and the list appends. It's garbage, not retained memory. It dropped to 8,632 B, about 575 B per publish, once
`TeventRouting.Dispatch` stopped allocating a delegate per dispatch (commit `c70e46199`, found while comparing with
[routing by convention](6-routing-by-convention-compared.md)). It drops further when the routes into components are
composed once per type, and if `ITaggregateTevent` stops claiming exactly-once delivery (see [later](4-later.md)).

## For the specification of memory in the final design

[Document 2](2-tevent-routing-decided-once-per-class.md) asks for a specification of the bytes a taggregate retains per
instance, with a bound. Measuring retained bytes in a specification is unreliable: `GC.GetTotalMemory` measures the
whole process, and Compze's specifications run in parallel. **The bytes allocated on the current thread by a load are a
better measure:** `GC.GetAllocatedBytesForCurrentThread()` sees no other thread. It bounds the retained bytes from above,
and moves with them: 1,872 B against 19,304 B at 15 components.

## All three changes combined

Measured on 2026-11-16 by taggregates-opus, on the branch `taggregates-opus/combined` (7dbfa74c0), in the worktree
`C:\Dev\Compze.worktrees\taggregates-opus-combined`. It merges this prototype (529a5170e) with
`taggregates-opus/snapshot-and-hook`. Written up in JAStudio's `dev_docs/taggregates/opus/the-prototypes-combined.md`,
in `C:\Dev\JAStudio.wt2`, branch `compze-taggretates`.

- **The two prototypes combine:** two small conflicts, both in `Taggregate`, and nothing in either had to change for the
  other. Compze's full suite passes on the combination.
- **A 15-component note retains 1,080 B, loaded from history or from a snapshot, and 8 B more once edited and
  committed.** Without this prototype's 144 B of scaffolding, a final design keeps about 936 B: some 200 B over plain
  objects, or about 75 MB for JAStudio's 80,000 notes, against about 59 MB as plain objects and 0.6 to 0.9 GB with
  0.6.0-alpha.
- **After the fix to `TeventRouting.Dispatch`** (c70e46199, merged into the combination at af602bd0e): a load allocates
  1,728 B from history and 1,984 B from a snapshot, and fifteen edits and a commit allocate 9,928 B, about 660 B an edit.
  Still 1,080 B retained, and +8 B after the edits.
- **What each part took:** per-class routing, from 11,256 B to 1,168 B; the lazy `TeventStream`, 24 B more; creating the
  lists of tevents only when needed, and handing them on whole, 64 B, and the 208 B their grown backing arrays kept after
  an edit.
