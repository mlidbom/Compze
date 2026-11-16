// Measures what Compze.Teventive's Taggregate costs per instance when tens of thousands are held in memory at once, as JAStudio holds its notes:
// the memory each keeps, the bytes allocated to make it, and the time, against plain objects holding the same data.
using System.Diagnostics;
using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;

const int noteCount = 80_000;

Measure("Plain objects, 4 parts", () => new PlainVocab(Guid.NewGuid(), "question", "answer", "explanation", "mnemonic", ["form"], ["noun"], ["part"], ["synonym"]));

foreach(var partCount in new[] { 0, 4 })
{
   Vocab.PartCount = partCount;
   Measure($"Taggregates hydrated from one stored-state tevent, {partCount} parts", () => Vocab.Hydrated(NewLoaded()));
   Measure($"Taggregates created by publishing, {partCount} parts", () => Vocab.Created(NewLoaded()));
}

Vocab.PartCount = 4;
var vocabs = Enumerable.Range(0, noteCount).Select(_ => Vocab.Hydrated(NewLoaded())).ToArray();
var received = 0;
foreach(var vocab in vocabs) ((ITaggregate)vocab).TeventStream.Subscribe(new CountingObserver(() => received++));

var before = Snapshot();
foreach(var vocab in vocabs) vocab.ChangeAnswer("another answer");
Report("One answer change published on each of the hydrated taggregates, 4 parts, one subscriber each", before, noteCount);
before = Snapshot();
foreach(var vocab in vocabs) vocab.ChangeAnswer("a third answer");
Report("A second answer change on each (its tevent type is resolved by now)", before, noteCount);
Console.WriteLine($"   tevents received by the subscribers: {received:N0}");
GC.KeepAlive(vocabs);

static VocabTevent.Loaded NewLoaded() => new(new TaggregateId(), "question", "answer", "explanation", "mnemonic", ["form"], ["noun"], ["part"], ["synonym"]);

static void Measure<T>(string what, Func<T> create) where T : class
{
   create(); // warm up: static constructors, reflection caches, the JIT
   var holder = new T[noteCount];
   var before = Snapshot();
   for(var index = 0; index < holder.Length; index++) holder[index] = create();
   Report(what, before, noteCount);
   GC.KeepAlive(holder);
}

static (long Retained, long Allocated, long Ticks, TimeSpan OwnCpu) Snapshot() =>
   (GC.GetTotalMemory(forceFullCollection: true), GC.GetTotalAllocatedBytes(precise: true), Stopwatch.GetTimestamp(), Process.GetCurrentProcess().TotalProcessorTime);

static void Report(string what, (long Retained, long Allocated, long Ticks, TimeSpan OwnCpu) before, int count)
{
   var elapsed = Stopwatch.GetElapsedTime(before.Ticks);
   var ownCpu = Process.GetCurrentProcess().TotalProcessorTime - before.OwnCpu;
   var allocated = GC.GetTotalAllocatedBytes(precise: true) - before.Allocated;
   var retained = GC.GetTotalMemory(forceFullCollection: true) - before.Retained;
   Console.WriteLine($"{what}:");
   Console.WriteLine($"   kept per instance: {retained / count,8:N0} B   ({retained / 1024.0 / 1024.0,7:N1} MB for {count:N0})");
   Console.WriteLine($"   allocated per instance: {allocated / count,8:N0} B");
   Console.WriteLine($"   time: {elapsed.TotalMilliseconds,8:N0} ms wall, {ownCpu.TotalMilliseconds,8:N0} ms of this process's processor time   ({elapsed.TotalMicroseconds / count:N1} µs each)");
}

sealed class CountingObserver(Action onNext) : IObserver<ITaggregateTevent<ITaggregateTevent>>
{
   public void OnCompleted() {}
   public void OnError(Exception error) {}
   public void OnNext(ITaggregateTevent<ITaggregateTevent> value) => onNext();
}

// ---- The same data as plain objects ----

sealed class PlainVocab(Guid id, string question, string answer, string explanation, string mnemonic, string[] forms, string[] partsOfSpeech, string[] compoundParts, string[] synonyms)
{
   public Guid Id { get; } = id;
   public string Question { get; private set; } = question;
   public string Answer { get; private set; } = answer;
   public string Explanation { get; private set; } = explanation;
   public string Mnemonic { get; private set; } = mnemonic;
   public PlainPart Forms { get; } = new(forms);
   public PlainPart PartsOfSpeech { get; } = new(partsOfSpeech);
   public PlainPart CompoundParts { get; } = new(compoundParts);
   public PlainPart Synonyms { get; } = new(synonyms);
}

sealed class PlainPart(string[] values)
{
   public string[] Values { get; private set; } = values;
   public bool IsLocked { get; private set; }
}

// ---- The tevents ----

interface IVocabTevent<out T> : ITaggregateTevent<T> where T : IVocabTevent;

interface IVocabTevent : ITaggregateTevent
{
   /// The stored state of a vocab, as one tevent: it is every property update at once.
   interface Loaded : ITaggregateCreatedTevent, PropertyUpdated.Question, PropertyUpdated.Answer, PropertyUpdated.Explanation, PropertyUpdated.Mnemonic,
                      Forms.PropertyUpdated.Values, PartsOfSpeech.PropertyUpdated.Values, CompoundParts.PropertyUpdated.Values, Synonyms.PropertyUpdated.Values;

   interface AnswerChanged : PropertyUpdated.Answer;

   interface PropertyUpdated : IVocabTevent
   {
      interface Question : PropertyUpdated { string Question { get; } }
      interface Answer : PropertyUpdated { string Answer { get; } }
      interface Explanation : PropertyUpdated { string Explanation { get; } }
      interface Mnemonic : PropertyUpdated { string Mnemonic { get; } }
   }

   interface Forms : IVocabTevent
   {
      interface Locked : Forms;
      interface PropertyUpdated : Forms { interface Values : PropertyUpdated { string[] FormValues { get; } } }
   }

   interface PartsOfSpeech : IVocabTevent
   {
      interface Locked : PartsOfSpeech;
      interface PropertyUpdated : PartsOfSpeech { interface Values : PropertyUpdated { string[] PartsOfSpeechValues { get; } } }
   }

   interface CompoundParts : IVocabTevent
   {
      interface Locked : CompoundParts;
      interface PropertyUpdated : CompoundParts { interface Values : PropertyUpdated { string[] CompoundPartsValues { get; } } }
   }

   interface Synonyms : IVocabTevent
   {
      interface Locked : Synonyms;
      interface PropertyUpdated : Synonyms { interface Values : PropertyUpdated { string[] SynonymsValues { get; } } }
   }
}

class VocabTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IVocabTevent<T> where T : IVocabTevent;

abstract class VocabTevent : TaggregateTevent, IVocabTevent
{
   protected VocabTevent() {}
   protected VocabTevent(TaggregateId id) : base(id) {}

   internal class Loaded(TaggregateId id, string question, string answer, string explanation, string mnemonic, string[] forms, string[] partsOfSpeech, string[] compoundParts, string[] synonyms)
      : VocabTevent(id), IVocabTevent.Loaded
   {
      public string Question { get; } = question;
      public string Answer { get; } = answer;
      public string Explanation { get; } = explanation;
      public string Mnemonic { get; } = mnemonic;
      public string[] FormValues { get; } = forms;
      public string[] PartsOfSpeechValues { get; } = partsOfSpeech;
      public string[] CompoundPartsValues { get; } = compoundParts;
      public string[] SynonymsValues { get; } = synonyms;
   }

   internal class AnswerChanged(string answer) : VocabTevent, IVocabTevent.AnswerChanged
   {
      public string Answer { get; } = answer;
   }

   internal abstract class Forms : VocabTevent, IVocabTevent.Forms { internal class Locked : Forms, IVocabTevent.Forms.Locked; }
   internal abstract class PartsOfSpeech : VocabTevent, IVocabTevent.PartsOfSpeech { internal class Locked : PartsOfSpeech, IVocabTevent.PartsOfSpeech.Locked; }
   internal abstract class CompoundParts : VocabTevent, IVocabTevent.CompoundParts { internal class Locked : CompoundParts, IVocabTevent.CompoundParts.Locked; }
   internal abstract class Synonyms : VocabTevent, IVocabTevent.Synonyms { internal class Locked : Synonyms, IVocabTevent.Synonyms.Locked; }
}

// ---- The taggregate ----

class Vocab : Taggregate<Vocab, IVocabTevent, VocabTevent, IVocabTevent<IVocabTevent>, VocabTevent<VocabTevent>>
{
   /// How many of the four parts a vocab is given, so that what one part costs can be told from what the root costs.
   internal static int PartCount = 4;

   public string Question { get; private set; } = "";
   public string Answer { get; private set; } = "";
   public string Explanation { get; private set; } = "";
   public string Mnemonic { get; private set; } = "";
   public FormsPart? Forms { get; }
   public PartsOfSpeechPart? PartsOfSpeech { get; }
   public CompoundPartsPart? CompoundParts { get; }
   public SynonymsPart? Synonyms { get; }

   Vocab() : base(TeventDispatcherConfig.IgnoreAllUnhandled)
   {
      RegisterTeventAppliers()
        .For<IVocabTevent.PropertyUpdated.Question>(it => Question = it.Question)
        .For<IVocabTevent.PropertyUpdated.Answer>(it => Answer = it.Answer)
        .For<IVocabTevent.PropertyUpdated.Explanation>(it => Explanation = it.Explanation)
        .For<IVocabTevent.PropertyUpdated.Mnemonic>(it => Mnemonic = it.Mnemonic);

      if(PartCount > 0)
      {
         Forms = new FormsPart(this);
         PartsOfSpeech = new PartsOfSpeechPart(this);
         CompoundParts = new CompoundPartsPart(this);
         Synonyms = new SynonymsPart(this);
      }
   }

   internal static Vocab Created(VocabTevent.Loaded state)
   {
      var vocab = new Vocab();
      vocab.Publish(state);
      return vocab;
   }

   /// Hydrates a vocab from its stored state without an event store. The version has to be set through a member marked for Compze's internal use:
   /// without it the vocab stays at version 0, and the next tevent it publishes is refused as not being a creation tevent.
   internal static Vocab Hydrated(VocabTevent.Loaded state)
   {
      ((IMutableTaggregateTevent)state).SetTaggregateVersionInternal(1);
      var vocab = new Vocab();
      ((ITaggregate)vocab).LoadFromHistory([TaggregateIdentifyingTevent.WrapIn(typeof(VocabTevent<>), state)]);
      return vocab;
   }

   internal void ChangeAnswer(string answer) => Publish(new VocabTevent.AnswerChanged(answer));

   internal class FormsPart : Component<FormsPart, IVocabTevent.Forms, VocabTevent.Forms>
   {
      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }

      public FormsPart(Vocab vocab) : base(vocab) =>
         RegisterTeventAppliers().For<IVocabTevent.Forms.PropertyUpdated.Values>(it => Values = it.FormValues)
                                 .For<IVocabTevent.Forms.Locked>(_ => IsLocked = true);
   }

   internal class PartsOfSpeechPart : Component<PartsOfSpeechPart, IVocabTevent.PartsOfSpeech, VocabTevent.PartsOfSpeech>
   {
      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }

      public PartsOfSpeechPart(Vocab vocab) : base(vocab) =>
         RegisterTeventAppliers().For<IVocabTevent.PartsOfSpeech.PropertyUpdated.Values>(it => Values = it.PartsOfSpeechValues)
                                 .For<IVocabTevent.PartsOfSpeech.Locked>(_ => IsLocked = true);
   }

   internal class CompoundPartsPart : Component<CompoundPartsPart, IVocabTevent.CompoundParts, VocabTevent.CompoundParts>
   {
      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }

      public CompoundPartsPart(Vocab vocab) : base(vocab) =>
         RegisterTeventAppliers().For<IVocabTevent.CompoundParts.PropertyUpdated.Values>(it => Values = it.CompoundPartsValues)
                                 .For<IVocabTevent.CompoundParts.Locked>(_ => IsLocked = true);
   }

   internal class SynonymsPart : Component<SynonymsPart, IVocabTevent.Synonyms, VocabTevent.Synonyms>
   {
      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }

      public SynonymsPart(Vocab vocab) : base(vocab) =>
         RegisterTeventAppliers().For<IVocabTevent.Synonyms.PropertyUpdated.Values>(it => Values = it.SynonymsValues)
                                 .For<IVocabTevent.Synonyms.Locked>(_ => IsLocked = true);
   }
}
