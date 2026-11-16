// Measures a vocab as today's Taggregate (Compze.Teventive 0.6.0-alpha from nuget.org) beside the same vocab on the prototype of tevent routing decided once
// for each class, in three shapes, and then runs the behaviour the proposal says the prototype has.
// The bytes are what to read: other work on the machine can't move them. The times are from one run, on a machine that other work shares.
using System.Diagnostics;
using Compze.Abstractions;
using Compze.Tessaging;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;
using TeventRoutingPerClass;

const int vocabCount = 80_000;
var stored = Enumerable.Range(0, vocabCount).Select(_ => new StoredVocab()).ToArray();

Console.WriteLine($"Loading {vocabCount:N0} vocabs from their stored state. The stored arrays and strings are shared with the stored state, and so are not in the bytes kept.");
Console.WriteLine();
Console.WriteLine($"{"",-74} {"kept",9} {"allocated",10} {"time",8}");

Measure("Plain objects, with four parts and four word sets", index => new PlainVocab(stored[index]));
foreach(var shape in Enum.GetValues<Shape>())
{
   Measure($"Today's Taggregate, {Describe(shape)}", index => TodayVocab.Loaded(stored[index], shape));
   Measure($"Routing per class,  {Describe(shape)}", index => PerClassVocab.Loaded(stored[index], shape));
}

Console.WriteLine();
Console.WriteLine("Publishing on each of the loaded vocabs with four components and four shared tomponents. Kept is what remains after Commit has taken the tevents.");
Console.WriteLine();
Console.WriteLine($"{"",-74} {"kept",9} {"allocated",10} {"time",8}");

const Shape fullShape = Shape.RootFourComponentsAndFourSharedTomponents;
var todayVocabs = stored.Select(it => TodayVocab.Loaded(it, fullShape)).ToArray();
var perClassVocabs = stored.Select(it => PerClassVocab.Loaded(it, fullShape)).ToArray();

MeasurePublishing("Today's Taggregate: an answer change, the first of its type",
                  index => todayVocabs[index].ChangeAnswer("another answer"), index => ((ITaggregate)todayVocabs[index]).Commit(_ => {}));
MeasurePublishing("Today's Taggregate: a second answer change",
                  index => todayVocabs[index].ChangeAnswer("a third answer"), index => ((ITaggregate)todayVocabs[index]).Commit(_ => {}));
MeasurePublishing("Today's Taggregate: a word set changed, through its slot in a component",
                  index => todayVocabs[index].Synonyms!.SeeAlso!.Set(["a", "b"]), index => ((ITaggregate)todayVocabs[index]).Commit(_ => {}));

MeasurePublishing("Routing per class:  an answer change, the first of its type",
                  index => perClassVocabs[index].ChangeAnswer("another answer"), index => perClassVocabs[index].Commit(_ => {}));
MeasurePublishing("Routing per class:  a second answer change",
                  index => perClassVocabs[index].ChangeAnswer("a third answer"), index => perClassVocabs[index].Commit(_ => {}));
MeasurePublishing("Routing per class:  a word set changed, through its slot in a component",
                  index => perClassVocabs[index].Synonyms!.SeeAlso!.Set(["a", "b"]), index => perClassVocabs[index].Commit(_ => {}));

Console.WriteLine();
Console.WriteLine($"   tevents handed to the prototype's hook: {PerClassVocab.TeventsHandedToTheHook:N0}");
GC.KeepAlive(todayVocabs);
GC.KeepAlive(perClassVocabs);

Console.WriteLine();
Console.WriteLine("What a TessageId costs, which every tevent of a taggregate makes today:");
Console.WriteLine();
Console.WriteLine($"{"",-74} {"kept",9} {"allocated",10} {"time",8}");
Measure("A TessageId", _ => new TessageId());

Console.WriteLine();
Console.WriteLine("The prototype's behaviour:");
Console.WriteLine();
PrototypeBehaviour.RunAll();
return;

static string Describe(Shape shape) => shape switch
{
   Shape.RootOnly              => "the root alone, four tevent appliers",
   Shape.RootAndFourComponents => "with four components of two tevent appliers each",
   _                           => "with four components and four shared tomponents"
};

static void Measure<T>(string what, Func<int, T> create) where T : class
{
   for(var index = 0; index < 5_000; index++) create(index); // warm up: static constructors, the resolved handlers of each class, the JIT
   var holder = new T[vocabCount];
   var before = Snapshot();
   for(var index = 0; index < holder.Length; index++) holder[index] = create(index);
   Report(what, before);
   GC.KeepAlive(holder);
}

static void MeasurePublishing(string what, Action<int> publish, Action<int> commit)
{
   var before = Snapshot();
   for(var index = 0; index < vocabCount; index++) publish(index);
   var publishing = (Allocated: GC.GetTotalAllocatedBytes(precise: true) - before.Allocated, Elapsed: Stopwatch.GetElapsedTime(before.Ticks));
   for(var index = 0; index < vocabCount; index++) commit(index);
   var kept = GC.GetTotalMemory(forceFullCollection: true) - before.Kept;
   Console.WriteLine($"{what,-74} {kept / vocabCount,7:N0} B {publishing.Allocated / vocabCount,8:N0} B {publishing.Elapsed.TotalMicroseconds / vocabCount,5:N2} µs");
}

static (long Kept, long Allocated, long Ticks) Snapshot() => (GC.GetTotalMemory(forceFullCollection: true), GC.GetTotalAllocatedBytes(precise: true), Stopwatch.GetTimestamp());

static void Report(string what, (long Kept, long Allocated, long Ticks) before)
{
   var elapsed = Stopwatch.GetElapsedTime(before.Ticks);
   var allocated = GC.GetTotalAllocatedBytes(precise: true) - before.Allocated;
   var kept = GC.GetTotalMemory(forceFullCollection: true) - before.Kept;
   Console.WriteLine($"{what,-74} {kept / vocabCount,7:N0} B {allocated / vocabCount,8:N0} B {elapsed.TotalMicroseconds / vocabCount,5:N2} µs");
}

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

// ---- What the proposal says of the prototype's behaviour, run ----

static class PrototypeBehaviour
{
   internal static void RunAll()
   {
      Observe("1. A publish from inside the hook is handed over after the call in progress returns, in the order published, and the hook is never inside itself",
              () =>
              {
                 var note = ReactingNote.Create();
                 note.OnHook = handedOver =>
                 {
                    if(handedOver.Any(it => it.Tevent is IReactingNoteTevent.Renamed { Name: "b" })) note.Rename("c");
                 };
                 note.Rename("b");
                 return $"hook calls: {string.Join(" | ", note.HookCalls)}; deepest nesting of the hook: {note.DeepestHookNesting}";
              });

      Observe("2. A tevent handler's tevent is handed over in the same call of the hook as the tevent it reacted to",
              () =>
              {
                 var note = ReactingNote.Create();
                 note.Rename("has a reaction");
                 return $"hook calls: {string.Join(" | ", note.HookCalls)}";
              });

      Observe("3. Committing from the hook, with a subscriber that publishes on the same note from inside the commit: nothing is lost, and the commits are in order",
              () =>
              {
                 var note = ReactingNote.Create();
                 var committed = new List<string>();
                 note.OnHook = _ => note.Commit(uncommitted =>
                 {
                    foreach(var wrapped in uncommitted)
                    {
                       committed.Add(ReactingNote.Describe(wrapped));
                       if(wrapped.Tevent is IReactingNoteTevent.Renamed { Name: "b" }) note.Rename("c");
                    }
                 });
                 note.Rename("b");
                 return $"committed: [{string.Join(", ", committed)}]; left uncommitted: {note.UncommittedCount()}";
              });

      Observe("4. A hook that throws: the next publish is handed over as usual, and nothing was lost to Commit",
              () =>
              {
                 var note = ReactingNote.Create();
                 note.OnHook = _ => throw new InvalidOperationException("the subscriber's contract failed");
                 var thrown = Thrown(() => note.Rename("b"));
                 note.OnHook = null;
                 note.HookCalls.Clear();
                 note.Rename("c");
                 var committed = new List<string>();
                 note.Commit(uncommitted => committed.AddRange(uncommitted.Select(ReactingNote.Describe)));
                 return $"thrown: {thrown}; the next publish's hook calls: {string.Join(" | ", note.HookCalls)}; committed afterwards: [{string.Join(", ", committed)}]";
              });

      Observe("5. A second thread that calls Publish while another thread is in it is refused",
              () =>
              {
                 var note = ReactingNote.Create();
                 using var theFirstThreadIsInTheHook = new ManualResetEventSlim();
                 using var theSecondThreadIsDone = new ManualResetEventSlim();
                 note.OnHook = _ =>
                 {
                    theFirstThreadIsInTheHook.Set();
                    theSecondThreadIsDone.Wait();
                 };
                 var first = Task.Run(() => note.Rename("from the first thread"));
                 theFirstThreadIsInTheHook.Wait();
                 note.OnHook = null;
                 var thrown = Thrown(() => note.Rename("from the second thread"));
                 theSecondThreadIsDone.Set();
                 first.Wait();
                 return $"the second thread's Publish threw: {thrown}; the note's name is \"{note.Name}\"";
              });

      Observe("6. Many threads at once, each loading and publishing on vocabs of its own, through the declarations all of them share",
              () =>
              {
                 var stored = Enumerable.Range(0, 20_000).Select(_ => new StoredVocab()).ToArray();
                 var vocabs = new PerClassVocab[stored.Length];
                 Parallel.For(0, stored.Length, index =>
                 {
                    vocabs[index] = PerClassVocab.Loaded(stored[index], Shape.RootFourComponentsAndFourSharedTomponents);
                    vocabs[index].ChangeAnswer("changed");
                    vocabs[index].Synonyms!.Antonyms!.Set(["changed"]);
                 });
                 var right = vocabs.Count(it => it is { Answer: "changed", Version: 7 } && it.Synonyms!.Antonyms!.Words is ["changed"] && it.Synonyms.SeeAlso!.Words is ["see also"]);
                 return $"{right:N0} of {vocabs.Length:N0} vocabs have the state their own thread gave them";
              });

      Observe("7. A slot declared below the declaration that enters it, and a lambda that captures: each is refused where the class is first used, with a message that says what to change",
              () => $"the slot: {Thrown(() => _ = new SlotDeclaredBelow())}; the lambda: {Thrown(CapturingDeclaration.Declare)}");
   }

   static void Observe(string claim, Func<string> observe)
   {
      Console.WriteLine(claim);
      Console.WriteLine($"   observed: {observe()}");
      Console.WriteLine();
   }

   static string Thrown(Action action)
   {
      try
      {
         action();
         return "nothing";
      }
      catch(Exception exception)
      {
         var innermost = exception;
         while(innermost.InnerException != null) innermost = innermost.InnerException;
         return $"{innermost.GetType().Name} (\"{innermost.Message}\")";
      }
   }
}

interface IReactingNoteTevent<out T> : ITaggregateTevent<T> where T : IReactingNoteTevent;

interface IReactingNoteTevent : ITaggregateTevent
{
   interface Created : ITaggregateCreatedTevent, IReactingNoteTevent;
   interface Renamed : IReactingNoteTevent { string Name { get; } }
   interface Reacted : IReactingNoteTevent;
}

class ReactingNoteTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IReactingNoteTevent<T> where T : IReactingNoteTevent;

abstract class ReactingNoteTevent : TaggregateTevent, IReactingNoteTevent
{
   protected ReactingNoteTevent() {}
   protected ReactingNoteTevent(TaggregateId id) : base(id) {}

   internal class Created(TaggregateId id) : ReactingNoteTevent(id), IReactingNoteTevent.Created;

   internal class Renamed(string name) : ReactingNoteTevent, IReactingNoteTevent.Renamed
   {
      public string Name { get; } = name;
   }

   internal class Reacted : ReactingNoteTevent, IReactingNoteTevent.Reacted;
}

/// A note with a tevent handler, as JAStudio's sentence has: a rename to "has a reaction" makes the handler publish a tevent of its own.
class ReactingNote : PerClassTaggregate<ReactingNote, IReactingNoteTevent, ReactingNoteTevent>
{
   static readonly TeventAppliers<ReactingNote, IReactingNoteTevent> Appliers = DeclareTeventAppliers(typeof(ReactingNoteTevent<>))
     .For<IReactingNoteTevent.Renamed>((note, tevent) => note.Name = tevent.Name)
     .IgnoreAllUnhandled();

   static readonly TeventAppliers<ReactingNote, IReactingNoteTevent> Handlers = DeclareTeventHandlers()
     .For<IReactingNoteTevent.Renamed>((note, tevent) =>
      {
         if(tevent.Name == "has a reaction") note.Publish(new ReactingNoteTevent.Reacted());
      });

   int _hookNesting;

   public string Name { get; private set; } = "";
   internal Action<IReadOnlyList<ITaggregateTevent<IReactingNoteTevent>>>? OnHook { get; set; }
   internal List<string> HookCalls { get; } = [];
   internal int DeepestHookNesting { get; private set; }

   ReactingNote() : base(Appliers, Handlers) {}

   internal static ReactingNote Create()
   {
      var note = new ReactingNote();
      note.Publish(new ReactingNoteTevent.Created(new TaggregateId()));
      note.Commit(_ => {});
      note.HookCalls.Clear();
      return note;
   }

   internal void Rename(string name) => Publish(new ReactingNoteTevent.Renamed(name));

   internal int UncommittedCount()
   {
      var count = 0;
      Commit(uncommitted => count = uncommitted.Count);
      return count;
   }

   internal static string Describe(ITaggregateTevent<IReactingNoteTevent> wrapped) => wrapped.Tevent switch
   {
      IReactingNoteTevent.Renamed renamed => $"Renamed(\"{renamed.Name}\") v{renamed.TaggregateVersion}",
      _                                   => $"{wrapped.Tevent.GetType().Name} v{wrapped.Tevent.TaggregateVersion}"
   };

   protected override void OnTeventsPublished(IReadOnlyList<ITaggregateTevent<IReactingNoteTevent>> publishedTevents)
   {
      DeepestHookNesting = Math.Max(DeepestHookNesting, ++_hookNesting);
      try
      {
         HookCalls.Add($"[{string.Join(", ", publishedTevents.Select(Describe))}]");
         OnHook?.Invoke(publishedTevents);
      }
      finally
      {
         _hookNesting--;
      }
   }
}

/// A component whose slot is declared below the declaration that enters it.
class SlotDeclaredBelow : PerClassComponent<SlotDeclaredBelow, IVocabTevent.Synonyms, VocabTevent.Synonyms>
{
   static readonly TeventAppliers<SlotDeclaredBelow, IVocabTevent.Synonyms> Appliers = DeclareTeventAppliers().SharedTomponent(SeeAlsoSlot);

   static readonly Slot<SlotDeclaredBelow, VocabTevent.Synonyms, IWordSetTevent, ISeeAlsoTevent<IWordSetTevent>> SeeAlsoSlot =
      DeclareSlot<IWordSetTevent, ISeeAlsoTevent<IWordSetTevent>>(typeof(SeeAlsoTevent<>), it => it.SeeAlso);

   public PerClassWordSet SeeAlso { get; }

   public SlotDeclaredBelow() : base(null!, Appliers) => SeeAlso = new PerClassWordSet(SeeAlsoSlot.In(this));
}

/// A declaration with a lambda that captures a local.
class CapturingDeclaration : PerClassComponent<CapturingDeclaration, IVocabTevent.Forms, VocabTevent.Forms>
{
   public bool IsLocked { get; private set; }

   CapturingDeclaration() : base(null!, null!) {}

   internal static void Declare()
   {
      var captured = true;
      DeclareTeventAppliers().For<IVocabTevent.Forms.Locked>((forms, _) => forms.IsLocked = captured);
   }
}
