# What was measured

**Status: draft, under review by the four sessions** (2026-11-16). On .NET 11, with workstation garbage collection,
over 10,000 to 80,000 instances. Bytes are exact and the same from run to run within a few bytes. Times are from a
machine that other work shares, at 10 to 13% load from that work, and the documents they come from say so per run.
Each team measured with a program of its own, and where they overlap, the numbers agree.

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
- **For JAStudio's notes,** with their real parts counted: about 30 MB more than plain objects, against about 1.1 GB
  today.
- **taggregates-fable's own prototype,** about 150 lines, measured the same, and found that a loaded tevent needs no
  wrapper object: an adopted tevent then loads in 232 B instead of about 350.

### Static slots in the real base classes (fable, `taggregate-experiments-fable-static-slots`)

Pending: jastudio-rep-fable is building it on the combined branch, with a slot in a component and a measurement of a
note with components and shared tomponents.

## How it was measured

- **Bytes kept:** `GC.GetTotalMemory(true)` before and after creating the instances, divided by their number.
- **Bytes allocated:** `GC.GetAllocatedBytesForCurrentThread()`.
- **The programs:** the opus team's in `../opus/benchmark/` (`generate.py`, and `generate_both.py` for the styles side by
  side), and taggregates-opus's `generate_combined.py` in JAStudio's `dev_docs/taggregates/opus/benchmark/`; the fable
  team's on `taggregate-experiments-fable`, under `experiments/taggregate-improvements-fable`.
- **For a specification in Compze,** bytes kept are unreliable: `GC.GetTotalMemory` measures the whole process, and
  Compze's specifications run in parallel. Bytes allocated on the specification's own thread are not, and bound the kept
  bytes from above: the performance specification should bound those, and the programs report the kept bytes and times.
