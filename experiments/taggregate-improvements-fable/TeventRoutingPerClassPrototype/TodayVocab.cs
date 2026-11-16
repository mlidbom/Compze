// The measured vocab as it is written against Compze.Teventive 0.6.0-alpha: registration in the constructors, a dispatcher for each teventive.
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses.Shared;

class TodayVocab : Taggregate<TodayVocab, IVocabTevent, VocabTevent, IVocabTevent<IVocabTevent>, VocabTevent<VocabTevent>>
{
   public string Question { get; private set; } = "";
   public string Answer { get; private set; } = "";
   public string Explanation { get; private set; } = "";
   public string Mnemonic { get; private set; } = "";
   public FormsPart? Forms { get; }
   public PartsOfSpeechPart? PartsOfSpeech { get; }
   public CompoundPartsPart? CompoundParts { get; }
   public SynonymsPart? Synonyms { get; }

   TodayVocab(Shape shape) : base(TeventDispatcherConfig.IgnoreAllUnhandled)
   {
      RegisterTeventAppliers()
        .For<IVocabTevent.PropertyUpdated.Question>(it => Question = it.Question)
        .For<IVocabTevent.PropertyUpdated.Answer>(it => Answer = it.Answer)
        .For<IVocabTevent.PropertyUpdated.Explanation>(it => Explanation = it.Explanation)
        .For<IVocabTevent.PropertyUpdated.Mnemonic>(it => Mnemonic = it.Mnemonic);

      if(shape == Shape.RootOnly) return;

      Forms = new FormsPart(this);
      PartsOfSpeech = new PartsOfSpeechPart(this);
      CompoundParts = new CompoundPartsPart(this);
      Synonyms = new SynonymsPart(this, withSharedTomponents: shape == Shape.RootFourComponentsAndFourSharedTomponents);
   }

   /// Loads a vocab from its stored state without a tevent store. The versions have to be set through a member marked for Compze's internal use, and the
   /// adopting wrapper tevents constructed by hand.
   internal static TodayVocab Loaded(StoredVocab stored, Shape shape)
   {
      var vocab = new TodayVocab(shape);
      var compactedHistory = new List<VocabTevent> { stored.Loaded() };
      if(shape == Shape.RootFourComponentsAndFourSharedTomponents)
      {
         compactedHistory.Add(new SeeAlsoTevent<WordSetTevent.WordsSet>(new WordSetTevent.WordsSet(stored.SeeAlso)));
         compactedHistory.Add(new AntonymsTevent<WordSetTevent.WordsSet>(new WordSetTevent.WordsSet(stored.Antonyms)));
         compactedHistory.Add(new ConfusedWithTevent<WordSetTevent.WordsSet>(new WordSetTevent.WordsSet(stored.ConfusedWith)));
         compactedHistory.Add(new PerfectSynonymsTevent<WordSetTevent.WordsSet>(new WordSetTevent.WordsSet(stored.PerfectSynonyms)));
      }

      var version = 0;
      foreach(var tevent in compactedHistory) ((IMutableTaggregateTevent)tevent).SetTaggregateVersionInternal(++version);
      ((ITaggregate)vocab).LoadFromHistory(compactedHistory.Select(tevent => TaggregateIdentifyingTevent.WrapIn(typeof(VocabTevent<>), tevent)));
      return vocab;
   }

   internal void ChangeAnswer(string answer) => Publish(new VocabTevent.AnswerChanged(answer));

   internal class FormsPart : Component<FormsPart, IVocabTevent.Forms, VocabTevent.Forms>
   {
      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }

      public FormsPart(TodayVocab vocab) : base(vocab) =>
         RegisterTeventAppliers().For<IVocabTevent.Forms.PropertyUpdated.Values>(it => Values = it.FormValues)
                                 .For<IVocabTevent.Forms.Locked>(_ => IsLocked = true);
   }

   internal class PartsOfSpeechPart : Component<PartsOfSpeechPart, IVocabTevent.PartsOfSpeech, VocabTevent.PartsOfSpeech>
   {
      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }

      public PartsOfSpeechPart(TodayVocab vocab) : base(vocab) =>
         RegisterTeventAppliers().For<IVocabTevent.PartsOfSpeech.PropertyUpdated.Values>(it => Values = it.PartsOfSpeechValues)
                                 .For<IVocabTevent.PartsOfSpeech.Locked>(_ => IsLocked = true);
   }

   internal class CompoundPartsPart : Component<CompoundPartsPart, IVocabTevent.CompoundParts, VocabTevent.CompoundParts>
   {
      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }

      public CompoundPartsPart(TodayVocab vocab) : base(vocab) =>
         RegisterTeventAppliers().For<IVocabTevent.CompoundParts.PropertyUpdated.Values>(it => Values = it.CompoundPartsValues)
                                 .For<IVocabTevent.CompoundParts.Locked>(_ => IsLocked = true);
   }

   internal class SynonymsPart : Component<SynonymsPart, IVocabTevent.Synonyms, VocabTevent.Synonyms>
   {
      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }
      public TodayWordSet? SeeAlso { get; }
      public TodayWordSet? Antonyms { get; }
      public TodayWordSet? ConfusedWith { get; }
      public TodayWordSet? PerfectSynonyms { get; }

      public SynonymsPart(TodayVocab vocab, bool withSharedTomponents) : base(vocab)
      {
         RegisterTeventAppliers().For<IVocabTevent.Synonyms.PropertyUpdated.Values>(it => Values = it.SynonymsValues)
                                 .For<IVocabTevent.Synonyms.Locked>(_ => IsLocked = true);

         if(!withSharedTomponents) return;

         SeeAlso = new TodayWordSet(new SharedTomponentSlot<IVocabTevent.Synonyms, VocabTevent.Synonyms, IWordSetTevent, ISeeAlsoTevent<IWordSetTevent>>(this, typeof(SeeAlsoTevent<IWordSetTevent>)));
         Antonyms = new TodayWordSet(new SharedTomponentSlot<IVocabTevent.Synonyms, VocabTevent.Synonyms, IWordSetTevent, IAntonymsTevent<IWordSetTevent>>(this, typeof(AntonymsTevent<IWordSetTevent>)));
         ConfusedWith = new TodayWordSet(new SharedTomponentSlot<IVocabTevent.Synonyms, VocabTevent.Synonyms, IWordSetTevent, IConfusedWithTevent<IWordSetTevent>>(this, typeof(ConfusedWithTevent<IWordSetTevent>)));
         PerfectSynonyms = new TodayWordSet(new SharedTomponentSlot<IVocabTevent.Synonyms, VocabTevent.Synonyms, IWordSetTevent, IPerfectSynonymsTevent<IWordSetTevent>>(this, typeof(PerfectSynonymsTevent<IWordSetTevent>)));
      }
   }
}

class TodayWordSet : SharedTomponent<IWordSetTevent>
{
   public string[] Words { get; private set; } = [];

   public TodayWordSet(ISharedTomponentSlot<IWordSetTevent> slot) : base(slot) =>
      RegisterTeventAppliers().For<IWordSetTevent.WordsSet>(it => Words = it.Words)
                              .For<IWordSetTevent.Cleared>(_ => Words = []);

   public void Set(string[] words) => Publish(new WordSetTevent.WordsSet(words));
}
