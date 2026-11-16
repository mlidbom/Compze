// The measured vocab as it would be written with tevent routing decided once for each class: a static declaration for each class, and nothing registered by an instance.
using Compze.Teventive.Taggregates.Tevents;
using TeventRoutingPerClass;

class PerClassVocab : PerClassTaggregate<PerClassVocab, IVocabTevent, VocabTevent>
{
   static readonly TeventAppliers<PerClassVocab, IVocabTevent> OwnAppliers = DeclareTeventAppliers(typeof(VocabTevent<>))
     .For<IVocabTevent.PropertyUpdated.Question>((vocab, tevent) => vocab.Question = tevent.Question)
     .For<IVocabTevent.PropertyUpdated.Answer>((vocab, tevent) => vocab.Answer = tevent.Answer)
     .For<IVocabTevent.PropertyUpdated.Explanation>((vocab, tevent) => vocab.Explanation = tevent.Explanation)
     .For<IVocabTevent.PropertyUpdated.Mnemonic>((vocab, tevent) => vocab.Mnemonic = tevent.Mnemonic)
     .IgnoreAllUnhandled();

   //A real class has one declaration. The measurement has two, so that a vocab without components can be measured beside one with them.
   static readonly TeventAppliers<PerClassVocab, IVocabTevent> AppliersWithComponents = OwnAppliers.ThenFor<PerClassVocab>()
     .Component(vocab => vocab.Forms!)
     .Component(vocab => vocab.PartsOfSpeech!)
     .Component(vocab => vocab.CompoundParts!)
     .Component(vocab => vocab.Synonyms!);

   public string Question { get; private set; } = "";
   public string Answer { get; private set; } = "";
   public string Explanation { get; private set; } = "";
   public string Mnemonic { get; private set; } = "";
   public FormsPart? Forms { get; }
   public PartsOfSpeechPart? PartsOfSpeech { get; }
   public CompoundPartsPart? CompoundParts { get; }
   public SynonymsPart? Synonyms { get; }

   ///<summary>What the vocab's collection would do when the vocab has published: here it only counts, so that the hook's cost is in the measurement.</summary>
   internal static long TeventsHandedToTheHook;

   PerClassVocab(Shape shape) : base(shape == Shape.RootOnly ? OwnAppliers : AppliersWithComponents)
   {
      if(shape == Shape.RootOnly) return;

      Forms = new FormsPart(this);
      PartsOfSpeech = new PartsOfSpeechPart(this);
      CompoundParts = new CompoundPartsPart(this);
      Synonyms = new SynonymsPart(this, withSharedTomponents: shape == Shape.RootFourComponentsAndFourSharedTomponents);
   }

   /// Loads a vocab from its stored state: each part gives its share of the compacted-history, and the root joins them.
   internal static PerClassVocab Loaded(StoredVocab stored, Shape shape)
   {
      var vocab = new PerClassVocab(shape);
      vocab.LoadFromCompactedHistory(shape == Shape.RootFourComponentsAndFourSharedTomponents
                                        ? [stored.Loaded(), ..SynonymsPart.CompactedHistoryOfTheSharedTomponents(stored)]
                                        : [stored.Loaded()]);
      return vocab;
   }

   internal void ChangeAnswer(string answer) => Publish(new VocabTevent.AnswerChanged(answer));

   protected override void OnTeventsPublished(IReadOnlyList<ITaggregateTevent<IVocabTevent>> publishedTevents) => TeventsHandedToTheHook += publishedTevents.Count;

   internal class FormsPart(PerClassVocab vocab) : PerClassComponent<FormsPart, IVocabTevent.Forms, VocabTevent.Forms>(vocab, Appliers)
   {
      static readonly TeventAppliers<FormsPart, IVocabTevent.Forms> Appliers = DeclareTeventAppliers()
        .For<IVocabTevent.Forms.PropertyUpdated.Values>((forms, tevent) => forms.Values = tevent.FormValues)
        .For<IVocabTevent.Forms.Locked>((forms, _) => forms.IsLocked = true);

      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }
   }

   internal class PartsOfSpeechPart(PerClassVocab vocab) : PerClassComponent<PartsOfSpeechPart, IVocabTevent.PartsOfSpeech, VocabTevent.PartsOfSpeech>(vocab, Appliers)
   {
      static readonly TeventAppliers<PartsOfSpeechPart, IVocabTevent.PartsOfSpeech> Appliers = DeclareTeventAppliers()
        .For<IVocabTevent.PartsOfSpeech.PropertyUpdated.Values>((partsOfSpeech, tevent) => partsOfSpeech.Values = tevent.PartsOfSpeechValues)
        .For<IVocabTevent.PartsOfSpeech.Locked>((partsOfSpeech, _) => partsOfSpeech.IsLocked = true);

      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }
   }

   internal class CompoundPartsPart(PerClassVocab vocab) : PerClassComponent<CompoundPartsPart, IVocabTevent.CompoundParts, VocabTevent.CompoundParts>(vocab, Appliers)
   {
      static readonly TeventAppliers<CompoundPartsPart, IVocabTevent.CompoundParts> Appliers = DeclareTeventAppliers()
        .For<IVocabTevent.CompoundParts.PropertyUpdated.Values>((compoundParts, tevent) => compoundParts.Values = tevent.CompoundPartsValues)
        .For<IVocabTevent.CompoundParts.Locked>((compoundParts, _) => compoundParts.IsLocked = true);

      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }
   }

   internal class SynonymsPart : PerClassComponent<SynonymsPart, IVocabTevent.Synonyms, VocabTevent.Synonyms>
   {
      //The slots are declared above the declarations that enter them: static fields are initialized in the order written.
      static readonly Slot<SynonymsPart, VocabTevent.Synonyms, IWordSetTevent, ISeeAlsoTevent<IWordSetTevent>> SeeAlsoSlot =
         DeclareSlot<IWordSetTevent, ISeeAlsoTevent<IWordSetTevent>>(typeof(SeeAlsoTevent<>), synonyms => synonyms.SeeAlso!);

      static readonly Slot<SynonymsPart, VocabTevent.Synonyms, IWordSetTevent, IAntonymsTevent<IWordSetTevent>> AntonymsSlot =
         DeclareSlot<IWordSetTevent, IAntonymsTevent<IWordSetTevent>>(typeof(AntonymsTevent<>), synonyms => synonyms.Antonyms!);

      static readonly Slot<SynonymsPart, VocabTevent.Synonyms, IWordSetTevent, IConfusedWithTevent<IWordSetTevent>> ConfusedWithSlot =
         DeclareSlot<IWordSetTevent, IConfusedWithTevent<IWordSetTevent>>(typeof(ConfusedWithTevent<>), synonyms => synonyms.ConfusedWith!);

      static readonly Slot<SynonymsPart, VocabTevent.Synonyms, IWordSetTevent, IPerfectSynonymsTevent<IWordSetTevent>> PerfectSynonymsSlot =
         DeclareSlot<IWordSetTevent, IPerfectSynonymsTevent<IWordSetTevent>>(typeof(PerfectSynonymsTevent<>), synonyms => synonyms.PerfectSynonyms!);

      static readonly TeventAppliers<SynonymsPart, IVocabTevent.Synonyms> OwnAppliers = DeclareTeventAppliers()
        .For<IVocabTevent.Synonyms.PropertyUpdated.Values>((synonyms, tevent) => synonyms.Values = tevent.SynonymsValues)
        .For<IVocabTevent.Synonyms.Locked>((synonyms, _) => synonyms.IsLocked = true);

      //A real class has one declaration: see PerClassVocab.
      static readonly TeventAppliers<SynonymsPart, IVocabTevent.Synonyms> AppliersWithSharedTomponents = OwnAppliers.ThenFor<SynonymsPart>()
        .SharedTomponent(SeeAlsoSlot)
        .SharedTomponent(AntonymsSlot)
        .SharedTomponent(ConfusedWithSlot)
        .SharedTomponent(PerfectSynonymsSlot);

      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }
      public PerClassWordSet? SeeAlso { get; }
      public PerClassWordSet? Antonyms { get; }
      public PerClassWordSet? ConfusedWith { get; }
      public PerClassWordSet? PerfectSynonyms { get; }

      public SynonymsPart(PerClassVocab vocab, bool withSharedTomponents) : base(vocab, withSharedTomponents ? AppliersWithSharedTomponents : OwnAppliers)
      {
         if(!withSharedTomponents) return;

         SeeAlso = new PerClassWordSet(SeeAlsoSlot.In(this));
         Antonyms = new PerClassWordSet(AntonymsSlot.In(this));
         ConfusedWith = new PerClassWordSet(ConfusedWithSlot.In(this));
         PerfectSynonyms = new PerClassWordSet(PerfectSynonymsSlot.In(this));
      }

      /// This part's share of a vocab's compacted-history: the state of each shared tomponent, adopted by its slot.
      internal static IEnumerable<VocabTevent> CompactedHistoryOfTheSharedTomponents(StoredVocab stored) =>
      [
         SeeAlsoSlot.Adopt(new WordSetTevent.WordsSet(stored.SeeAlso)),
         AntonymsSlot.Adopt(new WordSetTevent.WordsSet(stored.Antonyms)),
         ConfusedWithSlot.Adopt(new WordSetTevent.WordsSet(stored.ConfusedWith)),
         PerfectSynonymsSlot.Adopt(new WordSetTevent.WordsSet(stored.PerfectSynonyms))
      ];
   }
}

class PerClassWordSet(SlotOfAnOwner<IWordSetTevent> slot) : SharedTomponent<IWordSetTevent>(slot, Appliers)
{
   static readonly TeventAppliers<PerClassWordSet, IWordSetTevent> Appliers = DeclareTeventAppliers<PerClassWordSet>()
     .For<IWordSetTevent.WordsSet>((words, tevent) => words.Words = tevent.Words)
     .For<IWordSetTevent.Cleared>((words, _) => words.Words = []);

   public string[] Words { get; private set; } = [];

   public void Set(string[] words) => Publish(new WordSetTevent.WordsSet(words));
}
