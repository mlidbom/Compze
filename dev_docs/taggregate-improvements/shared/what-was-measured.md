# What was measured

**Status: agreed by all four sessions, then reviewed by two independent agents and revised with their agreement** (2026-11-16). On .NET 11, with workstation garbage collection,
over 10,000 to 80,000 instances. Bytes are exact and the same from run to run within a few bytes. Times are from a
machine that other work shares, at 3 to 13% load from that work depending on the run, and the documents they come from
say so per run.
Each team measured with a program of its own, and where they overlap, the numbers agree.

**The teams' programs referenced Debug builds of Compze.Teventive.** The independent Fable review reran them against
Release builds: kept bytes are the same, to the byte, in either build, while bytes allocated and times differ. Its
Release numbers are in [the Release rerun](#the-release-rerun-by-the-independent-review), below, and the README quotes
those.

## Today: Compze.Teventive 0.6.0-alpha

| What | Kept | Measured by |
|---|---:|---|
| A root with 4 tevent appliers | 1,632 B, against 96 B as a plain object | fable |
| Each component | about 974 B, against 40 B | fable |
| Each shared tomponent in its slot | about 1,170 B, against 24 B | fable |
| A note with 15 components of 3 properties, loaded | 11,457 B, against 733 B as plain objects | opus |
| The same, after one edit of each component | 7,094 B more | opus |

- **An instance keeps about 400 B more for every tevent type it meets,** for as long as it lives: each dispatcher resolves
  the type for itself and keeps the result.
- **For JAStudio's 80,000 notes:** 0.6 to 0.9 GB more than plain objects by the opus benchmark's shape, about 1.1 GB by
  taggregates-fable's count of the notes' real parts (a vocab about 12 components and 10 shared tomponents), on a heap
  of 733 MB today.

## The prototypes

### Routing per class, in the real base classes (opus, `taggregate-experiments-opus`)

A note with 15 components of 3 properties each, against today's per-instance registration in the same build:

| | Kept, loaded | Allocated by a load | Kept more after an edit of each component | Allocated by those edits |
|---|---:|---:|---:|---:|
| Plain objects | 733 B | | | |
| Registered per instance, as today | 11,256 B | 19,304 B | +6,488 B | 25,376 B |
| Declared per class | 1,168 B | 1,816 B | +216 B | 8,632 B |

- **Loading 80,000 such notes,** on one thread: 0.07 to 0.09 s declared, against 2.9 to 3.2 s registered per instance.
- **144 B of the 1,168 are the prototype's scaffolding,** its fallback to per-instance registration, which a final design
  doesn't have.
- **By convention instead of declared** (`taggregate-experiments-opus-convention`): the same bytes within a few, and the
  same load time. Setting up a note class and its 15 component classes the first time took 6.6 to 7.4 ms declared and
  12.6 to 14.8 ms by convention, so about 0.4 ms of reflection per class, once.

### All three changes combined (opus, `taggregates-opus/combined`)

Routing per class, the snapshot, the hook with its queue, and the lazy lists and `TeventStream`:

| | Kept, loaded | Allocated by a load | Kept more after an edit of each component and a commit |
|---|---:|---:|---:|
| 15 components, from history | 1,080 B | 1,728 B | +8 B |
| 15 components, from a snapshot | 1,080 B | 1,984 B | +8 B |
| 6 components, from a snapshot | 504 B, against 303 B as plain objects | | +8 B |
| 1 component, from a snapshot | 168 B, against 47 B as plain objects | | +8 B |

- **Without the 144 B of scaffolding,** a final design keeps about 936 B for the 15-component note: some 200 B over plain
  objects.
- **Fifteen edits and a commit allocate 9,928 B,** about 660 B an edit: the tevent with its `TessageId` and time stamp,
  its wrapper, and the cascade's list. Garbage, not kept.

### Routing per class with static slots (fable, `taggregate-experiments-fable`, standalone)

A vocab with four components and four shared tomponents, loaded from its stored state:

| | Kept | Allocated | Time |
|---|---:|---:|---:|
| Plain objects | 350 B | 352 B | 0.2 µs |
| Today's taggregate | 10,204 B | 19,648 B | 34 to 40 µs |
| The prototype | 584 B | 2,040 B | 2.6 to 2.8 µs |

- **Per teventive:** a root 168 B, a component 56 B, a shared tomponent 48 B, against 96, 40 and 24 B as plain objects.
- **For JAStudio's notes,** with their real parts counted: about 27 MB more than plain objects, by the review's
  arithmetic from JAStudio's part counts, against about 1.1 GB today.
- **taggregates-fable's own prototype,** about 150 lines, measured the same, and found that a loaded tevent needs no
  wrapper object: an adopted tevent then loads in 232 B instead of about 350.

### Static slots in the real base classes (fable, `taggregate-experiments-fable-static-slots`)

Built on the combined branch (commit `fb75d60a0`, from `604c00df4`), with 16 specifications. `Compze.Tests.Unit` passes,
496 of 496; the other test assemblies weren't run on this branch, since the change only adds to Compze.Teventive.
80,000 vocabs, each a root with four tevent appliers, four components of two tevent appliers, and four shared tomponents
of one class in one of the components, loaded from a snapshot of five tevents, with the machine at 3% load:

| | Kept | Allocated | Time |
|---|---:|---:|---:|
| Plain objects | 350 B | 352 B | 0.15 µs |
| Registered per instance, as today | 9,952 B | 22,104 B | 36.6 µs |
| Declared per class, a slot object for each slot of each instance | 832 B | 4,192 B | 4.4 µs |
| Declared per class, slots that belong to the class | 640 B | 4,800 B | 4.8 µs |

- **A slot that belongs to the class saves 48 B per slot:** 192 B for the four, and about 26 MB for JAStudio's notes,
  with about 10 slots in a vocab, 5 in a sentence and 6 in a kanji.
- **About 120 B of the 640 are the prototype's scaffolding,** so a final design keeps about 520 B: some 170 B over plain
  objects for a root, four components and four shared tomponents. That agrees with the standalone prototype: its 584 B
  less its 72 B of references to the declaration in each teventive is 512 B.
- **The static-slot row allocates 600 B more** than the slot-object row only because its adopted tevents are made through
  `PublisherTevent.WrapIn`, which allocates a closure on every call: see [bugs-and-findings.md](bugs-and-findings.md).
- **Publishing through a slot allocates 1,032 B** in both declared styles, and keeps nothing after `Commit`, against
  1,256 B registered per instance.

## The Release rerun, by the independent review

The Fable reviewer reran both teams' programs against Release builds of the prototypes, with the machine at 2 to 18%
other load:

| Slot shape: a vocab of a root, four components and four shared tomponents, one load | Debug, as stated above | Release, three runs |
|---|---:|---:|
| Allocated, slot object per instance | 4,192 B | 3,416 B |
| Allocated, slots of the class | 4,800 B | 3,770 to 3,800 B |
| Allocated, registered per instance | 22,104 B | 20,027 to 20,266 B |
| Allocated by a publish through a slot, declared | 1,032 B | 688 B |
| Time, registered per instance | 36.6 µs | 33 to 38 µs |
| Time, declared | 4.4 to 4.8 µs | 3.1 to 4.0 µs |

- **The slot shape, the nearest to JAStudio's, loads 9 to 12 times faster** declared than registered per instance.
- **The 15-component shape loads about 75 times faster:** 80,000 notes in 2.98 to 3.08 s registered, against 0.04 s
  declared. One run of three rounds.
- **Startup garbage is the largest remaining cost:** about 750 B allocated per loaded tevent in Release, so about 0.4 GB
  of short-lived garbage for JAStudio's 570,000 tevents at each start. It comes mostly from closures and `ScopedChange` on
  the load path, and from the wrappers, and the real implementation should avoid the closures.

## How it was measured

- **Bytes kept:** `GC.GetTotalMemory(true)` before and after creating the instances, divided by their number.
- **Bytes allocated:** `GC.GetAllocatedBytesForCurrentThread()` in the opus programs, and
  `GC.GetTotalAllocatedBytes(precise: true)` in the fable programs, in a process where one thread does the work.
- **The programs:** the opus team's in `../opus/benchmark/` (`generate.py`, and `generate_both.py` for the styles side by
  side), and taggregates-opus's `generate_combined.py` in JAStudio's `dev_docs/taggregates/opus/benchmark/`; the fable
  team's on `taggregate-experiments-fable`, under `experiments/taggregate-improvements-fable`.
- **For a specification in Compze,** bytes kept are unreliable: `GC.GetTotalMemory` measures the whole process, and
  Compze's specifications run in parallel. Bytes allocated on the specification's own thread are not, and bound the kept
  bytes from above: the performance specification should bound those, in Release, naming its configuration and leaving
  headroom, since allocated bytes depend on the build and vary a little between runs. The programs report the kept
  bytes and times.
