// Measures, on the combined prototype of the Taggregate improvements, a vocab with four components and four shared tomponents in one of them, in three styles:
// registered per instance, as Compze.Teventive 0.6.0-alpha has it; declared per class with a slot object for each slot of each instance; and declared per class
// with slots that belong to the class. Built against this worktree's Compze.Teventive: build src/Compze.Teventive first.
// The bytes are what to read: other work on the machine can't move them. The times are from one run, on a machine that other work shares.
using System.Diagnostics;
using Compze.Teventive;

const int vocabCount = 80_000;
var stored = Enumerable.Range(0, vocabCount).Select(_ => new StoredVocab()).ToArray();

Console.WriteLine($"Loading {vocabCount:N0} vocabs from a snapshot of five tevents: a root, four components, four shared tomponents in one component.");
Console.WriteLine("The stored arrays and strings are shared with the stored state, and so are not in the bytes kept.");
Console.WriteLine();
Console.WriteLine($"{"",-78} {"kept",9} {"allocated",10} {"time",8}");

Measure("Plain objects", index => new PlainVocab(stored[index]));
Measure("Registered per instance", index => PerInstanceVocab.Loaded(stored[index]));
Measure("Declared per class, a slot object for each slot of each instance", index => VocabWithASlotObjectForEachSlot.Loaded(stored[index]));
Measure("Declared per class, slots that belong to the class", index => VocabWithSlotsThatBelongToItsClasses.Loaded(stored[index]));

Console.WriteLine();
Console.WriteLine("Publishing on each of the loaded vocabs. Kept is what remains after Commit has taken the tevents.");
Console.WriteLine();
Console.WriteLine($"{"",-78} {"kept",9} {"allocated",10} {"time",8}");

var perInstance = stored.Select(PerInstanceVocab.Loaded).ToArray();
var withSlotObjects = stored.Select(VocabWithASlotObjectForEachSlot.Loaded).ToArray();
var withSlotsOfTheClass = stored.Select(VocabWithSlotsThatBelongToItsClasses.Loaded).ToArray();

MeasurePublishing("Registered per instance: a set of words changed through its slot, the first time",
                  index => perInstance[index].Synonyms!.SeeAlso!.Set(["a", "b"]), index => ((ITaggregate)perInstance[index]).Commit(_ => {}));
MeasurePublishing("Registered per instance: the same again",
                  index => perInstance[index].Synonyms!.SeeAlso!.Set(["c", "d"]), index => ((ITaggregate)perInstance[index]).Commit(_ => {}));
MeasurePublishing("A slot object for each slot: a set of words changed through its slot, the first time",
                  index => withSlotObjects[index].Synonyms.SeeAlso.Set(["a", "b"]), index => ((ITaggregate)withSlotObjects[index]).Commit(_ => {}));
MeasurePublishing("A slot object for each slot: the same again",
                  index => withSlotObjects[index].Synonyms.SeeAlso.Set(["c", "d"]), index => ((ITaggregate)withSlotObjects[index]).Commit(_ => {}));
MeasurePublishing("Slots that belong to the class: a set of words changed through its slot, the first time",
                  index => withSlotsOfTheClass[index].Synonyms.SeeAlso.Set(["a", "b"]), index => ((ITaggregate)withSlotsOfTheClass[index]).Commit(_ => {}));
MeasurePublishing("Slots that belong to the class: the same again",
                  index => withSlotsOfTheClass[index].Synonyms.SeeAlso.Set(["c", "d"]), index => ((ITaggregate)withSlotsOfTheClass[index]).Commit(_ => {}));

var allRight = withSlotsOfTheClass.All(it => it is { Question: "question", Version: 3 }
                                          && it.Synonyms is { Values: ["synonym"], SeeAlso.Words: ["c", "d"], Antonyms.Words: ["antonym"], ConfusedWith.Words: ["confused with"], PerfectSynonyms.Words: ["perfect synonym"] })
            && withSlotObjects.All(it => it.Synonyms is { SeeAlso.Words: ["c", "d"], Antonyms.Words: ["antonym"] })
            && perInstance.All(it => it.Synonyms is { SeeAlso.Words: ["c", "d"], Antonyms.Words: ["antonym"] });
Console.WriteLine();
Console.WriteLine($"Every vocab of every style has the state it was loaded with and then given: {allRight}");
GC.KeepAlive(perInstance);
GC.KeepAlive(withSlotObjects);
GC.KeepAlive(withSlotsOfTheClass);
return;

static void Measure<T>(string what, Func<int, T> create) where T : class
{
   for(var index = 0; index < 5_000; index++) create(index); // warm up: static constructors, the resolved routes of each class, the JIT
   var holder = new T[vocabCount];
   var before = Snapshot();
   for(var index = 0; index < holder.Length; index++) holder[index] = create(index);
   var elapsed = Stopwatch.GetElapsedTime(before.Ticks);
   var allocated = GC.GetTotalAllocatedBytes(precise: true) - before.Allocated;
   var kept = GC.GetTotalMemory(forceFullCollection: true) - before.Kept;
   Console.WriteLine($"{what,-78} {kept / vocabCount,7:N0} B {allocated / vocabCount,8:N0} B {elapsed.TotalMicroseconds / vocabCount,5:N2} µs");
   GC.KeepAlive(holder);
}

static void MeasurePublishing(string what, Action<int> publish, Action<int> commit)
{
   var before = Snapshot();
   for(var index = 0; index < vocabCount; index++) publish(index);
   var allocated = GC.GetTotalAllocatedBytes(precise: true) - before.Allocated;
   var elapsed = Stopwatch.GetElapsedTime(before.Ticks);
   for(var index = 0; index < vocabCount; index++) commit(index);
   var kept = GC.GetTotalMemory(forceFullCollection: true) - before.Kept;
   Console.WriteLine($"{what,-78} {kept / vocabCount,7:N0} B {allocated / vocabCount,8:N0} B {elapsed.TotalMicroseconds / vocabCount,5:N2} µs");
}

static (long Kept, long Allocated, long Ticks) Snapshot() => (GC.GetTotalMemory(forceFullCollection: true), GC.GetTotalAllocatedBytes(precise: true), Stopwatch.GetTimestamp());

// ---- The same data as plain objects ----

sealed class PlainVocab(StoredVocab stored)
{
   public Guid Id { get; } = stored.Id;
   public string Question { get; private set; } = stored.Question;
   public string Answer { get; private set; } = stored.Answer;
   public string Explanation { get; private set; } = stored.Explanation;
   public string Mnemonic { get; private set; } = stored.Mnemonic;
   public PlainPart Forms { get; } = new(stored.Forms);
   public PlainPart PartsOfSpeech { get; } = new(stored.PartsOfSpeech);
   public PlainPart CompoundParts { get; } = new(stored.CompoundParts);
   public PlainSynonymsPart Synonyms { get; } = new(stored);
}

class PlainPart(string[] values)
{
   public string[] Values { get; private set; } = values;
   public bool IsLocked { get; private set; }
}

sealed class PlainSynonymsPart(StoredVocab stored) : PlainPart(stored.Synonyms)
{
   public PlainWordSet SeeAlso { get; } = new(stored.SeeAlso);
   public PlainWordSet Antonyms { get; } = new(stored.Antonyms);
   public PlainWordSet ConfusedWith { get; } = new(stored.ConfusedWith);
   public PlainWordSet PerfectSynonyms { get; } = new(stored.PerfectSynonyms);
}

sealed class PlainWordSet(string[] words)
{
   public string[] Words { get; private set; } = words;
}
