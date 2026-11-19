# Routing by convention, compared with declaring it: the same cost, a different risk

> Written before the end review (2026-11-18): some names and API here have changed since; see
> [../shared/the-naming-pass.md](../shared/the-naming-pass.md) and
> [../shared/reviews/end-review-consolidated.md](../shared/reviews/end-review-consolidated.md).

Built and measured on 2026-11-16 by jastudio-rep-opus. An experiment, not for merging: branch
`taggregate-experiments-opus-convention`, in the worktree `C:\Dev\Compze.worktrees\taggregate-experiments-opus-convention`.
It's built on [the prototype of per-class routing](5-prototype-of-per-class-routing.md), so both ways run in one build.

## Conclusions

- **The cost decides nothing:** routing found by convention retains, allocates and loads the same as routing declared
  explicitly. Discovering a class's routing by reflection costs a fraction of a millisecond per class, once.
- **The convention reads shorter and plainer:** appliers are ordinary instance methods using `this`, and a class
  declares nothing else. No table, no static lambdas, no `.Component(…)` lines, no property override.
- **Its cost is a new kind of mistake, and one is silent:** a tevent handler whose method name is misspelled, or whose
  signature is slightly off, never runs, without an error. A declared table can't get that wrong.
- **I still recommend declaring the routing,** for that silent mistake, and because Compze declares its registrations
  explicitly everywhere else. But it's a taste call, and this experiment shows that either works. A third way, not
  prototyped, keeps most of the convention's ease without the silent mistake: see the end of this document.

## What the convention prototype does

A class carrying `[TeventRoutingByConvention]` gets its `TeventRouting` built once, by reflection, the first time any
instance needs it:

- **Its `Apply` methods are its tevent appliers,** and its `Handle` methods its tevent handlers. Each is an instance
  method, of any visibility, that returns `void` and takes one tevent.
- **Every field that holds a part is routed to:** a component, an entity collection or a shared-tomponent slot, being
  whatever implements `ITeventRoutedMember<TTevent>`. Auto-properties count, through their backing fields.
- **Inheritance:** each class from the root down contributes its own methods, then its own parts.
- **A method becomes a delegate once per class,** opened over its instance, and a field's getter is compiled once, so a
  dispatch costs what a declared route costs. The base classes look the class's routing up in a dictionary by its type,
  once per dispatch.

The attribute exists only so that the prototype leaves every other taggregate alone. In a final design the convention
would simply be how routing works.

## The same note, both ways

Declared, from [the per-class routing's specification](5-prototype-of-per-class-routing.md#the-api-as-written-in-the-prototypes-specification):

```csharp
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

By convention, from `ConventionVocabNote.cs` in the convention branch:

```csharp
[TeventRoutingByConvention]
class ConventionForms : TeventiveComponent<ConventionVocabNote, INoteTevent, NoteTevent, ConventionForms, IVocabTevent.Forms, VocabTevent.Forms>
{
   ...
   void Apply(IVocabTevent.Forms.Updated tevent)
   {
      All = tevent.AllForms.Split(',');
      Summary = $"{_vocab.Question} {tevent.AllForms}";
   }

   void Apply(IVocabTevent.Forms.Counted tevent) => Count = tevent.Count;

   void Handle(IVocabTevent.Forms.Updated _) => Publish(new VocabTevent.Forms.Counted(All.Count));
}
```

And the vocab, which declares its parts in the one style and only holds them in the other:

```csharp
//Declared:
static readonly TeventRouting<VocabNote> VocabNoteAppliers = NoteAppliers.Extend<VocabNote>(it => it
  .Component(static vocab => vocab.Forms)
  .Entities(static vocab => vocab._senses)
  .SharedTomponent(static vocab => vocab._audioSlot));

//By convention: nothing. The fields Forms, _senses and _audioSlot are found.
```

The same specifications pass for both: 17 of them, covering inheritance, a component typed on the subclass that declares
it, a component's own tevent handler, an entity collection, a shared tomponent, and loading from history.

## Measured

`benchmark/generate_both.py`, given `convention`, adds the convention style to the benchmark. Bytes per note, over 10,000
notes, after the fix to `TeventRouting.Dispatch` described below:

| Components | Style | Retained, loaded | Allocated by a load | Retained more after an edit of each component | Allocated by those edits |
|---:|---|---:|---:|---:|---:|
| 15 | registered per instance | 11,256 | 19,304 | +6,488 | 25,376 |
| 15 | declared per class | 1,168 | 1,816 | +216 | 8,632 |
| 15 | by convention | 1,180 | 1,816 | +224 | 8,659 |
| 6 | declared per class | 592 | 1,240 | +152 | 3,512 |
| 6 | by convention | 597 | 1,240 | +155 | 3,523 |
| 1 | declared per class | 272 | 920 | +120 | 664 |
| 1 | by convention | 273 | 920 | +120 | 666 |

The few bytes between the two per-class styles are within what `GC.GetTotalMemory` varies by between runs.

**Loading 80,000 notes of 15 components:** 0.07 to 0.09 s declared, 0.08 to 0.09 s by convention, against about 3 s
registered per instance. The machine averaged 12% processor load over the run.

**The first load of a class's notes, in a fresh process,** which includes declaring or discovering the routing of the
note's class and its 15 components' classes: 6.6 to 7.4 ms declared, 12.6 to 14.8 ms by
convention, over five fresh processes, with one outlier run of each at 9.2 and 25.6 ms. So discovering by reflection
costs about 6 to 7 ms for 16 classes, or about 0.4 ms per class: some 20 ms once for JAStudio's 50 or so classes. The
machine averaged 12% processor load over these runs. (The per-instance style's first load, about 35 ms, isn't
comparable: it ran first, and paid for compiling the code all three styles share.)

**A fix the comparison found:** `TeventRouting.Dispatch` passed a method group to `ConcurrentDictionary.GetOrAdd`, which
allocated a delegate on every dispatch. It now looks the routes up with `TryGetValue` first: commit `c70e46199` on the
per-class branch. That took 15 edits of a 15-component note from 10,312 B allocated to 8,632 B.

## What the convention gets wrong that a declaration can't

- **A misspelled or mis-shaped tevent handler is silently ignored.** `Handel(…)`, or a `Handle` with two parameters, is
  simply not a handler, and tevent handlers may go unhandled. A misspelled applier fails loudly at the first dispatch,
  as an unhandled tevent, since appliers must handle every tevent.
- **A field holding a part is taken to be a part of this class.** A reference to another taggregate's component, or to a
  sibling component, would be routed to. Within a component, a sibling's tevents never arrive, so a sibling reference is
  harmless there, but in a taggregate, a field referring to another taggregate's part would misroute its tevents.
- **A shared tomponent's slot must be kept in a field of its full type.** The routing needs the adopting wrapper tevent
  type per class, and only the slot's type says it. Declaring has the same need, so this costs the convention nothing.
- **Nothing in the code calls an `Apply` method,** so the IDE reports each as unused, and finding what applies a tevent
  means searching by name. A declared table is code the IDE navigates.
- **Several appliers matching one tevent run in declaration order,** taken from metadata order: what the C# compiler
  emits, though not what the language promises.

## What the convention does better

- **Plain instance methods:** `this` works as usual, so there's no static-lambda discipline and no check that handlers
  hold no state.
- **Nothing to declare:** no table, no property override, and no `.Component(…)` line to forget (though forgetting one
  is loud anyway).

## A third way, not prototyped: attributes on the methods

Instance methods marked `[TeventApplier]` and `[TeventHandler]`, found by reflection as in the convention prototype,
with the parts declared or found as now. That keeps the convention's plain instance methods, and removes its worst
mistake: a marked method whose signature is wrong can fail loudly when the routing is built, and the name is free. With
JetBrains' `[MeansImplicitUse]` on the attribute classes, the IDE stops calling the methods unused. The parts are best
still declared, so no field is taken for a part by accident.
