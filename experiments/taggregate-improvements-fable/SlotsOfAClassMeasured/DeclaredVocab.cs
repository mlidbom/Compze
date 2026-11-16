// The measured vocab with its tevent routing declared once for each class, in two styles of slot:
// a SharedTomponentSlot object for each slot of each instance, as the combined prototype has it, and slots that belong to the class, in static fields.
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses.Shared;

/// A component without shared tomponents, typed on whichever class of vocab holds it.
class FormsPart<TVocab>(TVocab vocab) : TeventiveComponent<TVocab, IVocabTevent, VocabTevent, FormsPart<TVocab>, IVocabTevent.Forms, VocabTevent.Forms>(vocab)
   where TVocab : ITeventiveInternals<IVocabTevent, VocabTevent>
{
   static readonly TeventRouting<FormsPart<TVocab>> Appliers = TeventRouting<FormsPart<TVocab>>.DeclareAppliers(it => it
     .For<IVocabTevent.Forms.PropertyUpdated.Values>(static (forms, tevent) => forms.Values = tevent.FormValues)
     .For<IVocabTevent.Forms.Locked>(static (forms, _) => forms.IsLocked = true));

   protected override TeventRouting TeventAppliers => Appliers;

   public string[] Values { get; private set; } = [];
   public bool IsLocked { get; private set; }
}

class PartsOfSpeechPart<TVocab>(TVocab vocab) : TeventiveComponent<TVocab, IVocabTevent, VocabTevent, PartsOfSpeechPart<TVocab>, IVocabTevent.PartsOfSpeech, VocabTevent.PartsOfSpeech>(vocab)
   where TVocab : ITeventiveInternals<IVocabTevent, VocabTevent>
{
   static readonly TeventRouting<PartsOfSpeechPart<TVocab>> Appliers = TeventRouting<PartsOfSpeechPart<TVocab>>.DeclareAppliers(it => it
     .For<IVocabTevent.PartsOfSpeech.PropertyUpdated.Values>(static (partsOfSpeech, tevent) => partsOfSpeech.Values = tevent.PartsOfSpeechValues)
     .For<IVocabTevent.PartsOfSpeech.Locked>(static (partsOfSpeech, _) => partsOfSpeech.IsLocked = true));

   protected override TeventRouting TeventAppliers => Appliers;

   public string[] Values { get; private set; } = [];
   public bool IsLocked { get; private set; }
}

class CompoundPartsPart<TVocab>(TVocab vocab) : TeventiveComponent<TVocab, IVocabTevent, VocabTevent, CompoundPartsPart<TVocab>, IVocabTevent.CompoundParts, VocabTevent.CompoundParts>(vocab)
   where TVocab : ITeventiveInternals<IVocabTevent, VocabTevent>
{
   static readonly TeventRouting<CompoundPartsPart<TVocab>> Appliers = TeventRouting<CompoundPartsPart<TVocab>>.DeclareAppliers(it => it
     .For<IVocabTevent.CompoundParts.PropertyUpdated.Values>(static (compoundParts, tevent) => compoundParts.Values = tevent.CompoundPartsValues)
     .For<IVocabTevent.CompoundParts.Locked>(static (compoundParts, _) => compoundParts.IsLocked = true));

   protected override TeventRouting TeventAppliers => Appliers;

   public string[] Values { get; private set; } = [];
   public bool IsLocked { get; private set; }
}

/// A set of words, in either style of slot.
class DeclaredWordSet : SharedTomponent<IWordSetTevent>
{
   static readonly TeventRouting<DeclaredWordSet> Appliers = TeventRouting<DeclaredWordSet>.DeclareAppliers(it => it
     .For<IWordSetTevent.WordsSet>(static (words, tevent) => words.Words = tevent.Words)
     .For<IWordSetTevent.Cleared>(static (words, _) => words.Words = []));

   protected override TeventRouting TeventAppliers => Appliers;

   public DeclaredWordSet(ISharedTomponentSlot<IWordSetTevent> slot) : base(slot) {}
   public DeclaredWordSet(SharedTomponentSlotInAnOwner<IWordSetTevent> slot) : base(slot) {}

   public string[] Words { get; private set; } = [];

   public void Set(string[] words) => Publish(new WordSetTevent.WordsSet(words));
}

// ---- A slot object for each slot of each instance: the combined prototype's style ----

class VocabWithASlotObjectForEachSlot : Taggregate<VocabWithASlotObjectForEachSlot, IVocabTevent, VocabTevent, IVocabTevent<IVocabTevent>, VocabTevent<VocabTevent>>
{
   static readonly TeventRouting<VocabWithASlotObjectForEachSlot> Appliers = TeventRouting<VocabWithASlotObjectForEachSlot>.DeclareAppliers(it => it
     .For<IVocabTevent.PropertyUpdated.Question>(static (vocab, tevent) => vocab.Question = tevent.Question)
     .For<IVocabTevent.PropertyUpdated.Answer>(static (vocab, tevent) => vocab.Answer = tevent.Answer)
     .For<IVocabTevent.PropertyUpdated.Explanation>(static (vocab, tevent) => vocab.Explanation = tevent.Explanation)
     .For<IVocabTevent.PropertyUpdated.Mnemonic>(static (vocab, tevent) => vocab.Mnemonic = tevent.Mnemonic)
     .Component(static vocab => vocab.Forms)
     .Component(static vocab => vocab.PartsOfSpeech)
     .Component(static vocab => vocab.CompoundParts)
     .Component(static vocab => vocab.Synonyms));

   protected override TeventRouting TeventAppliers => Appliers;

   public string Question { get; private set; } = "";
   public string Answer { get; private set; } = "";
   public string Explanation { get; private set; } = "";
   public string Mnemonic { get; private set; } = "";
   public FormsPart<VocabWithASlotObjectForEachSlot> Forms { get; }
   public PartsOfSpeechPart<VocabWithASlotObjectForEachSlot> PartsOfSpeech { get; }
   public CompoundPartsPart<VocabWithASlotObjectForEachSlot> CompoundParts { get; }
   public SynonymsPart Synonyms { get; }

   VocabWithASlotObjectForEachSlot()
   {
      Forms = new FormsPart<VocabWithASlotObjectForEachSlot>(this);
      PartsOfSpeech = new PartsOfSpeechPart<VocabWithASlotObjectForEachSlot>(this);
      CompoundParts = new CompoundPartsPart<VocabWithASlotObjectForEachSlot>(this);
      Synonyms = new SynonymsPart(this);
   }

   /// Loads a vocab from a snapshot, with the adopting wrapper tevents constructed by hand.
   internal static VocabWithASlotObjectForEachSlot Loaded(StoredVocab stored)
   {
      var vocab = new VocabWithASlotObjectForEachSlot();
      ((ITaggregate)vocab).LoadFromSnapshot([
                                               stored.Loaded(),
                                               new SeeAlsoTevent<WordSetTevent.WordsSet>(new WordSetTevent.WordsSet(stored.SeeAlso)),
                                               new AntonymsTevent<WordSetTevent.WordsSet>(new WordSetTevent.WordsSet(stored.Antonyms)),
                                               new ConfusedWithTevent<WordSetTevent.WordsSet>(new WordSetTevent.WordsSet(stored.ConfusedWith)),
                                               new PerfectSynonymsTevent<WordSetTevent.WordsSet>(new WordSetTevent.WordsSet(stored.PerfectSynonyms))
                                            ],
                                            version: 1);
      return vocab;
   }

   internal void ChangeAnswer(string answer) => Publish(new VocabTevent.AnswerChanged(answer));

   internal class SynonymsPart : TeventiveComponent<VocabWithASlotObjectForEachSlot, IVocabTevent, VocabTevent, SynonymsPart, IVocabTevent.Synonyms, VocabTevent.Synonyms>
   {
      static readonly TeventRouting<SynonymsPart> Appliers = TeventRouting<SynonymsPart>.DeclareAppliers(it => it
        .For<IVocabTevent.Synonyms.PropertyUpdated.Values>(static (synonyms, tevent) => synonyms.Values = tevent.SynonymsValues)
        .For<IVocabTevent.Synonyms.Locked>(static (synonyms, _) => synonyms.IsLocked = true)
        .SharedTomponent(static synonyms => synonyms._seeAlsoSlot)
        .SharedTomponent(static synonyms => synonyms._antonymsSlot)
        .SharedTomponent(static synonyms => synonyms._confusedWithSlot)
        .SharedTomponent(static synonyms => synonyms._perfectSynonymsSlot));

      protected override TeventRouting TeventAppliers => Appliers;

      readonly SharedTomponentSlot<IVocabTevent.Synonyms, VocabTevent.Synonyms, IWordSetTevent, ISeeAlsoTevent<IWordSetTevent>> _seeAlsoSlot;
      readonly SharedTomponentSlot<IVocabTevent.Synonyms, VocabTevent.Synonyms, IWordSetTevent, IAntonymsTevent<IWordSetTevent>> _antonymsSlot;
      readonly SharedTomponentSlot<IVocabTevent.Synonyms, VocabTevent.Synonyms, IWordSetTevent, IConfusedWithTevent<IWordSetTevent>> _confusedWithSlot;
      readonly SharedTomponentSlot<IVocabTevent.Synonyms, VocabTevent.Synonyms, IWordSetTevent, IPerfectSynonymsTevent<IWordSetTevent>> _perfectSynonymsSlot;

      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }
      public DeclaredWordSet SeeAlso { get; }
      public DeclaredWordSet Antonyms { get; }
      public DeclaredWordSet ConfusedWith { get; }
      public DeclaredWordSet PerfectSynonyms { get; }

      public SynonymsPart(VocabWithASlotObjectForEachSlot vocab) : base(vocab)
      {
         _seeAlsoSlot = new(this, typeof(SeeAlsoTevent<IWordSetTevent>));
         _antonymsSlot = new(this, typeof(AntonymsTevent<IWordSetTevent>));
         _confusedWithSlot = new(this, typeof(ConfusedWithTevent<IWordSetTevent>));
         _perfectSynonymsSlot = new(this, typeof(PerfectSynonymsTevent<IWordSetTevent>));
         SeeAlso = new DeclaredWordSet(_seeAlsoSlot);
         Antonyms = new DeclaredWordSet(_antonymsSlot);
         ConfusedWith = new DeclaredWordSet(_confusedWithSlot);
         PerfectSynonyms = new DeclaredWordSet(_perfectSynonymsSlot);
      }
   }
}

// ---- Slots that belong to the class, in static fields: this experiment ----

class VocabWithSlotsThatBelongToItsClasses : Taggregate<VocabWithSlotsThatBelongToItsClasses, IVocabTevent, VocabTevent, IVocabTevent<IVocabTevent>, VocabTevent<VocabTevent>>
{
   static readonly TeventRouting<VocabWithSlotsThatBelongToItsClasses> Appliers = TeventRouting<VocabWithSlotsThatBelongToItsClasses>.DeclareAppliers(it => it
     .For<IVocabTevent.PropertyUpdated.Question>(static (vocab, tevent) => vocab.Question = tevent.Question)
     .For<IVocabTevent.PropertyUpdated.Answer>(static (vocab, tevent) => vocab.Answer = tevent.Answer)
     .For<IVocabTevent.PropertyUpdated.Explanation>(static (vocab, tevent) => vocab.Explanation = tevent.Explanation)
     .For<IVocabTevent.PropertyUpdated.Mnemonic>(static (vocab, tevent) => vocab.Mnemonic = tevent.Mnemonic)
     .Component(static vocab => vocab.Forms)
     .Component(static vocab => vocab.PartsOfSpeech)
     .Component(static vocab => vocab.CompoundParts)
     .Component(static vocab => vocab.Synonyms));

   protected override TeventRouting TeventAppliers => Appliers;

   public string Question { get; private set; } = "";
   public string Answer { get; private set; } = "";
   public string Explanation { get; private set; } = "";
   public string Mnemonic { get; private set; } = "";
   public FormsPart<VocabWithSlotsThatBelongToItsClasses> Forms { get; }
   public PartsOfSpeechPart<VocabWithSlotsThatBelongToItsClasses> PartsOfSpeech { get; }
   public CompoundPartsPart<VocabWithSlotsThatBelongToItsClasses> CompoundParts { get; }
   public SynonymsPart Synonyms { get; }

   VocabWithSlotsThatBelongToItsClasses()
   {
      Forms = new FormsPart<VocabWithSlotsThatBelongToItsClasses>(this);
      PartsOfSpeech = new PartsOfSpeechPart<VocabWithSlotsThatBelongToItsClasses>(this);
      CompoundParts = new CompoundPartsPart<VocabWithSlotsThatBelongToItsClasses>(this);
      Synonyms = new SynonymsPart(this);
   }

   /// Loads a vocab from a snapshot: each part gives its share, and the slots make the adopted tevents.
   internal static VocabWithSlotsThatBelongToItsClasses Loaded(StoredVocab stored)
   {
      var vocab = new VocabWithSlotsThatBelongToItsClasses();
      ((ITaggregate)vocab).LoadFromSnapshot([stored.Loaded(), ..SynonymsPart.SnapshotOfTheSharedTomponents(stored)], version: 1);
      return vocab;
   }

   internal void ChangeAnswer(string answer) => Publish(new VocabTevent.AnswerChanged(answer));

   internal class SynonymsPart : TeventiveComponent<VocabWithSlotsThatBelongToItsClasses, IVocabTevent, VocabTevent, SynonymsPart, IVocabTevent.Synonyms, VocabTevent.Synonyms>
   {
      //The slots are declared above the routing that enters them: static fields are initialized in the order written.
      static readonly SharedTomponentSlotOfAClass<SynonymsPart, IVocabTevent.Synonyms, VocabTevent.Synonyms, IWordSetTevent, ISeeAlsoTevent<IWordSetTevent>> SeeAlsoSlot =
         new(typeof(SeeAlsoTevent<>), static synonyms => synonyms.SeeAlso);

      static readonly SharedTomponentSlotOfAClass<SynonymsPart, IVocabTevent.Synonyms, VocabTevent.Synonyms, IWordSetTevent, IAntonymsTevent<IWordSetTevent>> AntonymsSlot =
         new(typeof(AntonymsTevent<>), static synonyms => synonyms.Antonyms);

      static readonly SharedTomponentSlotOfAClass<SynonymsPart, IVocabTevent.Synonyms, VocabTevent.Synonyms, IWordSetTevent, IConfusedWithTevent<IWordSetTevent>> ConfusedWithSlot =
         new(typeof(ConfusedWithTevent<>), static synonyms => synonyms.ConfusedWith);

      static readonly SharedTomponentSlotOfAClass<SynonymsPart, IVocabTevent.Synonyms, VocabTevent.Synonyms, IWordSetTevent, IPerfectSynonymsTevent<IWordSetTevent>> PerfectSynonymsSlot =
         new(typeof(PerfectSynonymsTevent<>), static synonyms => synonyms.PerfectSynonyms);

      static readonly TeventRouting<SynonymsPart> Appliers = TeventRouting<SynonymsPart>.DeclareAppliers(it => it
        .For<IVocabTevent.Synonyms.PropertyUpdated.Values>(static (synonyms, tevent) => synonyms.Values = tevent.SynonymsValues)
        .For<IVocabTevent.Synonyms.Locked>(static (synonyms, _) => synonyms.IsLocked = true)
        .SharedTomponent(SeeAlsoSlot)
        .SharedTomponent(AntonymsSlot)
        .SharedTomponent(ConfusedWithSlot)
        .SharedTomponent(PerfectSynonymsSlot));

      protected override TeventRouting TeventAppliers => Appliers;

      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }
      public DeclaredWordSet SeeAlso { get; }
      public DeclaredWordSet Antonyms { get; }
      public DeclaredWordSet ConfusedWith { get; }
      public DeclaredWordSet PerfectSynonyms { get; }

      public SynonymsPart(VocabWithSlotsThatBelongToItsClasses vocab) : base(vocab)
      {
         SeeAlso = new DeclaredWordSet(SeeAlsoSlot.In(this));
         Antonyms = new DeclaredWordSet(AntonymsSlot.In(this));
         ConfusedWith = new DeclaredWordSet(ConfusedWithSlot.In(this));
         PerfectSynonyms = new DeclaredWordSet(PerfectSynonymsSlot.In(this));
      }

      internal static IEnumerable<VocabTevent> SnapshotOfTheSharedTomponents(StoredVocab stored) =>
      [
         SeeAlsoSlot.Adopt(new WordSetTevent.WordsSet(stored.SeeAlso)),
         AntonymsSlot.Adopt(new WordSetTevent.WordsSet(stored.Antonyms)),
         ConfusedWithSlot.Adopt(new WordSetTevent.WordsSet(stored.ConfusedWith)),
         PerfectSynonymsSlot.Adopt(new WordSetTevent.WordsSet(stored.PerfectSynonyms))
      ];
   }
}
