// Checks run for the independent review of dev_docs/taggregate-improvements/shared, against the static-slots prototype
// (taggregate-experiments-fable-static-slots, fb75d60a0): each prints what the prototype does where the shared conclusions
// say something else, or say nothing.
using Compze.Abstractions;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses.Shared;
using Compze.Teventive.Taggregates.Tevents;

Check("1. An observer throws while a cascade of two tevents is handed on: is the second tevent ever handed on?", () =>
{
   var note = Note.Create("created");
   note.ReactToEditsInATeventHandler();
   var observed = new List<string>();
   var hasThrown = false;
   ((ITaggregate)note).TeventStream.Subscribe(it =>
   {
      observed.Add($"{it.Tevent.GetType().Name} v{it.Tevent.TaggregateVersion}");
      if(it.Tevent is INoteTevent.Edited && !hasThrown)
      {
         hasThrown = true;
         throw new IOException("The observer, as a tevent store's session might, failed to save.");
      }
   });

   try { note.Edit("edited"); }
   catch(IOException exception) { Console.WriteLine($"   Edit threw: {exception.Message}"); }

   note.Mark();
   Console.WriteLine($"   The note is at version {note.Version}, and still accepts publishing.");
   Console.WriteLine($"   The observer was told of: {string.Join(", ", observed)}");
   Console.WriteLine($"   Reacted v3 was {(observed.Any(it => it.StartsWith("Reacted")) ? "handed on" : "NEVER handed on: applied, uncommitted, and skipped by the observer")}.");
});

Check("2. A root whose table forgets its component's entry, loaded from a snapshot tevent that the root also applies", () =>
{
   var vocab = VocabThatForgetsItsComponent.LoadFromSnapshot(new TaggregateId(), "question", ["form 1", "form 2"]);
   Console.WriteLine($"   Loaded without an exception. Question: '{vocab.Question}'. Forms restored: {vocab.Forms.Values.Count} of 2.");
   try
   {
      vocab.Forms.Set(["form 3"]);
      Console.WriteLine("   Publishing through the component succeeded.");
   }
   catch(Exception exception) { Console.WriteLine($"   Publishing through the component is loud: {exception.GetType().Name}"); }
});

Check("3. A component that constructs its two shared tomponents with each other's slots", () =>
{
   var vocab = VocabWithSwappedSlots.Create("question");
   vocab.Related.Synonyms.Add("a synonym");
   Console.WriteLine($"   After Synonyms.Add: Synonyms has {vocab.Related.Synonyms.Words.Count} words, Antonyms has {vocab.Related.Antonyms.Words.Count}. No exception.");
});

Check("4. An observer that publishes on the taggregate it observes: on the publishing thread, and from another thread while the publishing thread waits for it", () =>
{
   var onTheSameThread = Note.Create("created");
   ((ITaggregate)onTheSameThread).TeventStream.Subscribe(it =>
   {
      if(it.Tevent is INoteTevent.Edited) onTheSameThread.Mark();
   });
   onTheSameThread.Edit("edited");
   Console.WriteLine($"   On the publishing thread: fine, version {onTheSameThread.Version}.");

   var fromAnotherThread = Note.Create("created");
   ((ITaggregate)fromAnotherThread).TeventStream.Subscribe(it =>
   {
      //What TeventStoreUpdater.OnTaggregateTevent does: it blocks on PublishAsync, whose participation handlers are awaited, and so may run on other threads.
      if(it.Tevent is INoteTevent.Edited) Task.Run(() => fromAnotherThread.Mark()).GetAwaiter().GetResult();
   });
   try
   {
      fromAnotherThread.Edit("edited");
      Console.WriteLine($"   From another thread while the publishing thread waits: fine, version {fromAnotherThread.Version}.");
   }
   catch(Exception exception) { Console.WriteLine($"   From another thread while the publishing thread waits: {exception.GetType().Name}: {exception.Message}"); }
});

Check("5. A table entry for a tevent type outside the class's own tevents", () =>
{
   //That this compiles is the finding: today's For<THandledTevent> is constrained to the class's tevent type, and TeventRouting<TInstance>.Declaration.For isn't.
   var note = Note.Create("created");
   note.Edit("edited");
   Console.WriteLine($"   It compiled, and it never ran: the applier for IForeignTevent was called {Note.TimesTheForeignApplierRan} times.");
});

Check("6. Commit called from inside a tevent handler, in a cascade whose invariants then fail", () =>
{
   var note = Note.Create("created");
   note.CommitInsideItsTeventHandler();
   try { note.Edit(Note.TextThatBreaksTheInvariants); }
   catch(Exception exception) { Console.WriteLine($"   Edit threw: {exception.GetType().Name}"); }

   Console.WriteLine($"   Committed from inside the handler, before the invariants were asserted: {string.Join(", ", note.CommittedInsideTheHandler)}");
   try
   {
      ((ITaggregate)note).Commit(_ => {});
      Console.WriteLine("   A later Commit is accepted.");
   }
   catch(Exception exception) { Console.WriteLine($"   A later Commit is refused: {exception.GetType().Name}"); }
});

Check("7. A subclass that adds a tevent applier and forgets to override TeventAppliers", () =>
{
   var note = SubclassThatForgetsItsTable.CreateSubclass("created");
   note.Edit("edited");
   Console.WriteLine($"   No exception. The subclass's applier ran {note.TimesTheSubclassApplierRan} times: no prototype asserts the instance's exact class.");
});
return;

static void Check(string what, Action check)
{
   Console.WriteLine(what);
   try { check(); }
   catch(Exception exception) { Console.WriteLine($"   UNEXPECTED {exception.GetType().Name}: {exception.Message}"); }

   Console.WriteLine();
}

// ---- A note: tevent handlers, the invariants, and what the checks switch on ----

interface IForeignTevent : ITevent;

interface INoteTevent : ITaggregateTevent
{
   interface TextUpdated : INoteTevent { string Text { get; } }
   interface Created : ITaggregateCreatedTevent, TextUpdated;
   interface Edited : TextUpdated;
   interface Reacted : INoteTevent;
   interface Marked : INoteTevent;
}

interface INoteTevent<out T> : ITaggregateTevent<T> where T : INoteTevent;
class NoteTevent<T>(T tevent) : TaggregateTevent<T>(tevent), INoteTevent<T> where T : INoteTevent;

class NoteTevent : TaggregateTevent, INoteTevent
{
   protected NoteTevent() {}
   NoteTevent(TaggregateId taggregateId) : base(taggregateId) {}

   internal class Created(TaggregateId taggregateId, string text) : NoteTevent(taggregateId), INoteTevent.Created { public string Text { get; } = text; }
   internal class Edited(string text) : NoteTevent, INoteTevent.Edited { public string Text { get; } = text; }
   internal class Reacted : NoteTevent, INoteTevent.Reacted;
   internal class Marked : NoteTevent, INoteTevent.Marked;
}

class Note : Taggregate<Note, INoteTevent, NoteTevent, INoteTevent<INoteTevent>, NoteTevent<NoteTevent>>
{
   protected static readonly TeventRouting<Note> NoteAppliers = TeventRouting<Note>.DeclareAppliers(it => it
     .For<INoteTevent.TextUpdated>(static (note, tevent) => note.Text = tevent.Text)
     .For<INoteTevent.Reacted>(static (_, _) => {})
     .For<INoteTevent.Marked>(static (_, _) => {})
     .For<IForeignTevent>(static (_, _) => TimesTheForeignApplierRan++));

   static readonly TeventRouting<Note> NoteHandlers = TeventRouting<Note>.DeclareHandlers(it => it
     .For<INoteTevent.Edited>(static (note, _) =>
      {
         if(note._commitsInsideItsTeventHandler)
            ((ITaggregate)note).Commit(tevents => note.CommittedInsideTheHandler.AddRange(tevents.Select(tevent => $"{tevent.Tevent.GetType().Name} v{tevent.Tevent.TaggregateVersion}")));
         if(note._reactsToEditsInATeventHandler) note.Publish(new NoteTevent.Reacted());
      }));

   protected override TeventRouting TeventAppliers => NoteAppliers;
   protected override TeventRouting TeventHandlers => NoteHandlers;

   public const string TextThatBreaksTheInvariants = "a text that breaks the invariants";
   public static int TimesTheForeignApplierRan;

   public string Text { get; private set; } = "";
   bool _reactsToEditsInATeventHandler;
   bool _commitsInsideItsTeventHandler;
   public void ReactToEditsInATeventHandler() => _reactsToEditsInATeventHandler = true;
   public void CommitInsideItsTeventHandler() => _commitsInsideItsTeventHandler = true;
   public List<string> CommittedInsideTheHandler { get; } = [];

   public static Note Create(string text)
   {
      var note = new Note();
      note.Publish(new NoteTevent.Created(new TaggregateId(), text));
      return note;
   }

   public void Edit(string text) => Publish(new NoteTevent.Edited(text));
   public void Mark() => Publish(new NoteTevent.Marked());

   protected override void AssertInvariantsAreMet()
   {
      if(Text == TextThatBreaksTheInvariants) throw new InvalidOperationException("The invariants don't hold.");
   }
}

class SubclassThatForgetsItsTable : Note
{
   // ReSharper disable once UnusedMember.Local The mistake this class is here to make: the table is declared, and TeventAppliers isn't overridden.
   static readonly TeventRouting<SubclassThatForgetsItsTable> SubclassAppliers = NoteAppliers.Extend<SubclassThatForgetsItsTable>(it => it
     .For<INoteTevent.Edited>(static (note, _) => note.TimesTheSubclassApplierRan++));

   public int TimesTheSubclassApplierRan { get; private set; }

   public static SubclassThatForgetsItsTable CreateSubclass(string text)
   {
      var note = new SubclassThatForgetsItsTable();
      note.Publish(new NoteTevent.Created(new TaggregateId(), text));
      return note;
   }
}

// ---- A vocab with a component, and a component with two shared tomponents of one class ----

interface IVocabTevent : ITaggregateTevent
{
   interface QuestionUpdated : IVocabTevent { string Question { get; } }
   interface Created : ITaggregateCreatedTevent, QuestionUpdated;

   interface Forms : IVocabTevent
   {
      interface Updated : Forms { IReadOnlyList<string> Values { get; } }
   }

   interface Related : IVocabTevent;

   ///<summary>One tevent for the vocab and its component, as the-design.md describes: it implements every property-updated tevent of both.</summary>
   interface Snapshot : ITaggregateSnapshotTevent, QuestionUpdated, Forms.Updated;
}

interface IVocabTevent<out T> : ITaggregateTevent<T> where T : IVocabTevent;
class VocabTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IVocabTevent<T> where T : IVocabTevent;

class VocabTevent : TaggregateTevent, IVocabTevent
{
   protected VocabTevent() {}
   VocabTevent(TaggregateId taggregateId) : base(taggregateId) {}

   internal class Created(TaggregateId taggregateId, string question) : VocabTevent(taggregateId), IVocabTevent.Created { public string Question { get; } = question; }

   internal class Snapshot(TaggregateId taggregateId, string question, IReadOnlyList<string> forms) : VocabTevent(taggregateId), IVocabTevent.Snapshot
   {
      public string Question { get; } = question;
      public IReadOnlyList<string> Values { get; } = forms;
   }

   internal abstract class Forms : VocabTevent, IVocabTevent.Forms
   {
      internal class Updated(IReadOnlyList<string> values) : Forms, IVocabTevent.Forms.Updated { public IReadOnlyList<string> Values { get; } = values; }
   }

   internal abstract class Related : VocabTevent, IVocabTevent.Related;
}

class VocabThatForgetsItsComponent : Taggregate<VocabThatForgetsItsComponent, IVocabTevent, VocabTevent, IVocabTevent<IVocabTevent>, VocabTevent<VocabTevent>>
{
   //The mistake this class is here to make: no .Component(static vocab => vocab.Forms) entry.
   static readonly TeventRouting<VocabThatForgetsItsComponent> Appliers = TeventRouting<VocabThatForgetsItsComponent>.DeclareAppliers(it => it
     .For<IVocabTevent.QuestionUpdated>(static (vocab, tevent) => vocab.Question = tevent.Question));

   protected override TeventRouting TeventAppliers => Appliers;

   VocabThatForgetsItsComponent() => Forms = new VocabForms(this);

   public string Question { get; private set; } = "";
   public VocabForms Forms { get; }

   public static VocabThatForgetsItsComponent LoadFromSnapshot(TaggregateId id, string question, IReadOnlyList<string> forms)
   {
      var vocab = new VocabThatForgetsItsComponent();
      ((ITaggregate)vocab).LoadFromSnapshot([new VocabTevent.Snapshot(id, question, forms)], version: 1);
      return vocab;
   }
}

class VocabForms : TeventiveComponent<VocabThatForgetsItsComponent, IVocabTevent, VocabTevent, VocabForms, IVocabTevent.Forms, VocabTevent.Forms>
{
   static readonly TeventRouting<VocabForms> Appliers = TeventRouting<VocabForms>.DeclareAppliers(it => it
     .For<IVocabTevent.Forms.Updated>(static (forms, tevent) => forms.Values = tevent.Values));

   protected override TeventRouting TeventAppliers => Appliers;

   public VocabForms(VocabThatForgetsItsComponent vocab) : base(vocab) {}

   public IReadOnlyList<string> Values { get; private set; } = [];

   public void Set(IReadOnlyList<string> values) => Publish(new VocabTevent.Forms.Updated(values));
}

interface IWordSetTevent : ITevent
{
   interface Added : IWordSetTevent { string Word { get; } }
}

class WordSetTevent : IWordSetTevent
{
   internal class Added(string word) : WordSetTevent, IWordSetTevent.Added { public string Word { get; } = word; }
}

interface ISynonymsTevent<out T> : IVocabTevent.Related, IPublisherTevent<T> where T : IWordSetTevent;
class SynonymsTevent<T>(T tevent) : VocabTevent.Related, ISynonymsTevent<T> where T : IWordSetTevent { public T Tevent { get; } = tevent; }

interface IAntonymsTevent<out T> : IVocabTevent.Related, IPublisherTevent<T> where T : IWordSetTevent;
class AntonymsTevent<T>(T tevent) : VocabTevent.Related, IAntonymsTevent<T> where T : IWordSetTevent { public T Tevent { get; } = tevent; }

class VocabWithSwappedSlots : Taggregate<VocabWithSwappedSlots, IVocabTevent, VocabTevent, IVocabTevent<IVocabTevent>, VocabTevent<VocabTevent>>
{
   static readonly TeventRouting<VocabWithSwappedSlots> Appliers = TeventRouting<VocabWithSwappedSlots>.DeclareAppliers(it => it
     .For<IVocabTevent.QuestionUpdated>(static (vocab, tevent) => vocab.Question = tevent.Question)
     .Component(static vocab => vocab.Related));

   protected override TeventRouting TeventAppliers => Appliers;

   VocabWithSwappedSlots() => Related = new RelatedWithSwappedSlots(this);

   public string Question { get; private set; } = "";
   public RelatedWithSwappedSlots Related { get; }

   public static VocabWithSwappedSlots Create(string question)
   {
      var vocab = new VocabWithSwappedSlots();
      vocab.Publish(new VocabTevent.Created(new TaggregateId(), question));
      return vocab;
   }
}

class RelatedWithSwappedSlots : TeventiveComponent<VocabWithSwappedSlots, IVocabTevent, VocabTevent, RelatedWithSwappedSlots, IVocabTevent.Related, VocabTevent.Related>
{
   static readonly SharedTomponentSlotOfAClass<RelatedWithSwappedSlots, IVocabTevent.Related, VocabTevent.Related, IWordSetTevent, ISynonymsTevent<IWordSetTevent>> SynonymsSlot =
      new(typeof(SynonymsTevent<>), static related => related.Synonyms);

   static readonly SharedTomponentSlotOfAClass<RelatedWithSwappedSlots, IVocabTevent.Related, VocabTevent.Related, IWordSetTevent, IAntonymsTevent<IWordSetTevent>> AntonymsSlot =
      new(typeof(AntonymsTevent<>), static related => related.Antonyms);

   static readonly TeventRouting<RelatedWithSwappedSlots> Appliers = TeventRouting<RelatedWithSwappedSlots>.DeclareAppliers(it => it
     .SharedTomponent(SynonymsSlot)
     .SharedTomponent(AntonymsSlot));

   protected override TeventRouting TeventAppliers => Appliers;

   public RelatedWithSwappedSlots(VocabWithSwappedSlots vocab) : base(vocab)
   {
      //The mistake this class is here to make: each shared tomponent is constructed with the other's slot.
      Synonyms = new WordSet(AntonymsSlot.In(this));
      Antonyms = new WordSet(SynonymsSlot.In(this));
   }

   public WordSet Synonyms { get; }
   public WordSet Antonyms { get; }
}

class WordSet : SharedTomponent<IWordSetTevent>
{
   static readonly TeventRouting<WordSet> Appliers = TeventRouting<WordSet>.DeclareAppliers(it => it
     .For<IWordSetTevent.Added>(static (words, tevent) => words._words.Add(tevent.Word)));

   protected override TeventRouting TeventAppliers => Appliers;

   readonly List<string> _words = [];

   public WordSet(SharedTomponentSlotInAnOwner<IWordSetTevent> slot) : base(slot) {}

   public IReadOnlyList<string> Words => _words;

   public void Add(string word) => Publish(new WordSetTevent.Added(word));
}
