// The measured vocab registered per instance, as against Compze.Teventive 0.6.0-alpha: registration in the constructors, a dispatcher for each teventive.
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses.Shared;

class PerInstanceVocab : Taggregate<PerInstanceVocab, IVocabTevent, VocabTevent, IVocabTevent<IVocabTevent>, VocabTevent<VocabTevent>>
{
   public string Question { get; private set; } = "";
   public string Answer { get; private set; } = "";
   public string Explanation { get; private set; } = "";
   public string Mnemonic { get; private set; } = "";
   public FormsPart? Forms { get; }
   public PartsOfSpeechPart? PartsOfSpeech { get; }
   public CompoundPartsPart? CompoundParts { get; }
   public SynonymsPart? Synonyms { get; }

   PerInstanceVocab() : base(TeventDispatcherConfig.IgnoreAllUnhandled)
   {
      RegisterTeventAppliers()
        .For<IVocabTevent.PropertyUpdated.Question>(it => Question = it.Question)
        .For<IVocabTevent.PropertyUpdated.Answer>(it => Answer = it.Answer)
        .For<IVocabTevent.PropertyUpdated.Explanation>(it => Explanation = it.Explanation)
        .For<IVocabTevent.PropertyUpdated.Mnemonic>(it => Mnemonic = it.Mnemonic);

      Forms = new FormsPart(this);
      PartsOfSpeech = new PartsOfSpeechPart(this);
      CompoundParts = new CompoundPartsPart(this);
      Synonyms = new SynonymsPart(this);
   }

   /// Loads a vocab from a snapshot, with the adopting wrapper tevents constructed by hand.
   internal static PerInstanceVocab Loaded(StoredVocab stored)
   {
      var vocab = new PerInstanceVocab();
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

   internal class FormsPart : Component<FormsPart, IVocabTevent.Forms, VocabTevent.Forms>
   {
      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }

      public FormsPart(PerInstanceVocab vocab) : base(vocab) =>
         RegisterTeventAppliers().For<IVocabTevent.Forms.PropertyUpdated.Values>(it => Values = it.FormValues)
                                 .For<IVocabTevent.Forms.Locked>(_ => IsLocked = true);
   }

   internal class PartsOfSpeechPart : Component<PartsOfSpeechPart, IVocabTevent.PartsOfSpeech, VocabTevent.PartsOfSpeech>
   {
      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }

      public PartsOfSpeechPart(PerInstanceVocab vocab) : base(vocab) =>
         RegisterTeventAppliers().For<IVocabTevent.PartsOfSpeech.PropertyUpdated.Values>(it => Values = it.PartsOfSpeechValues)
                                 .For<IVocabTevent.PartsOfSpeech.Locked>(_ => IsLocked = true);
   }

   internal class CompoundPartsPart : Component<CompoundPartsPart, IVocabTevent.CompoundParts, VocabTevent.CompoundParts>
   {
      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }

      public CompoundPartsPart(PerInstanceVocab vocab) : base(vocab) =>
         RegisterTeventAppliers().For<IVocabTevent.CompoundParts.PropertyUpdated.Values>(it => Values = it.CompoundPartsValues)
                                 .For<IVocabTevent.CompoundParts.Locked>(_ => IsLocked = true);
   }

   internal class SynonymsPart : Component<SynonymsPart, IVocabTevent.Synonyms, VocabTevent.Synonyms>
   {
      public string[] Values { get; private set; } = [];
      public bool IsLocked { get; private set; }
      public PerInstanceWordSet? SeeAlso { get; }
      public PerInstanceWordSet? Antonyms { get; }
      public PerInstanceWordSet? ConfusedWith { get; }
      public PerInstanceWordSet? PerfectSynonyms { get; }

      public SynonymsPart(PerInstanceVocab vocab) : base(vocab)
      {
         RegisterTeventAppliers().For<IVocabTevent.Synonyms.PropertyUpdated.Values>(it => Values = it.SynonymsValues)
                                 .For<IVocabTevent.Synonyms.Locked>(_ => IsLocked = true);

         SeeAlso = new PerInstanceWordSet(new SharedTomponentSlot<IVocabTevent.Synonyms, VocabTevent.Synonyms, IWordSetTevent, ISeeAlsoTevent<IWordSetTevent>>(this, typeof(SeeAlsoTevent<IWordSetTevent>)));
         Antonyms = new PerInstanceWordSet(new SharedTomponentSlot<IVocabTevent.Synonyms, VocabTevent.Synonyms, IWordSetTevent, IAntonymsTevent<IWordSetTevent>>(this, typeof(AntonymsTevent<IWordSetTevent>)));
         ConfusedWith = new PerInstanceWordSet(new SharedTomponentSlot<IVocabTevent.Synonyms, VocabTevent.Synonyms, IWordSetTevent, IConfusedWithTevent<IWordSetTevent>>(this, typeof(ConfusedWithTevent<IWordSetTevent>)));
         PerfectSynonyms = new PerInstanceWordSet(new SharedTomponentSlot<IVocabTevent.Synonyms, VocabTevent.Synonyms, IWordSetTevent, IPerfectSynonymsTevent<IWordSetTevent>>(this, typeof(PerfectSynonymsTevent<IWordSetTevent>)));
      }
   }
}

class PerInstanceWordSet : SharedTomponent<IWordSetTevent>
{
   public string[] Words { get; private set; } = [];

   public PerInstanceWordSet(ISharedTomponentSlot<IWordSetTevent> slot) : base(slot) =>
      RegisterTeventAppliers().For<IWordSetTevent.WordsSet>(it => Words = it.Words)
                              .For<IWordSetTevent.Cleared>(_ => Words = []);

   public void Set(string[] words) => Publish(new WordSetTevent.WordsSet(words));
}
