// The tevents of the measured vocab. Today's Taggregate and the prototype publish and apply the same ones, so that only the routing differs.
// The root's tevents and its four components' are the JAStudio session's (TaggregateCost/Program.cs). The shared tomponent, a set of words, and the
// adopting wrapper tevents of its four slots in the synonyms component are added here.
using Compze.Abstractions;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;

interface IVocabTevent<out T> : ITaggregateTevent<T> where T : IVocabTevent;

interface IVocabTevent : ITaggregateTevent
{
   /// The stored state of a vocab's root and components, as one tevent: it is every property update at once.
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

// ---- The shared tomponent's tevents: a set of words ----

interface IWordSetTevent : ITevent
{
   interface WordsSet : IWordSetTevent { string[] Words { get; } }
   interface Cleared : IWordSetTevent;
}

class WordSetTevent : IWordSetTevent
{
   internal class WordsSet(string[] words) : WordSetTevent, IWordSetTevent.WordsSet
   {
      public string[] Words { get; } = words;
   }

   internal class Cleared : WordSetTevent, IWordSetTevent.Cleared;
}

// ---- The adopting wrapper tevents of the four slots in the synonyms component: an interface and a class for each slot ----

interface ISeeAlsoTevent<out T> : IVocabTevent.Synonyms, IPublisherTevent<T> where T : IWordSetTevent;
class SeeAlsoTevent<T>(T tevent) : VocabTevent.Synonyms, ISeeAlsoTevent<T> where T : IWordSetTevent { public T Tevent { get; } = tevent; }

interface IAntonymsTevent<out T> : IVocabTevent.Synonyms, IPublisherTevent<T> where T : IWordSetTevent;
class AntonymsTevent<T>(T tevent) : VocabTevent.Synonyms, IAntonymsTevent<T> where T : IWordSetTevent { public T Tevent { get; } = tevent; }

interface IConfusedWithTevent<out T> : IVocabTevent.Synonyms, IPublisherTevent<T> where T : IWordSetTevent;
class ConfusedWithTevent<T>(T tevent) : VocabTevent.Synonyms, IConfusedWithTevent<T> where T : IWordSetTevent { public T Tevent { get; } = tevent; }

interface IPerfectSynonymsTevent<out T> : IVocabTevent.Synonyms, IPublisherTevent<T> where T : IWordSetTevent;
class PerfectSynonymsTevent<T>(T tevent) : VocabTevent.Synonyms, IPerfectSynonymsTevent<T> where T : IWordSetTevent { public T Tevent { get; } = tevent; }

/// What a measured vocab is made of, so that what a component costs, and what a shared tomponent costs, can be told from what the root costs.
enum Shape
{
   RootOnly,
   RootAndFourComponents,
   RootFourComponentsAndFourSharedTomponents
}

/// A vocab's stored state, as its file would hold it.
sealed class StoredVocab
{
   public Guid Id { get; } = Guid.NewGuid();
   public string Question => "question";
   public string Answer => "answer";
   public string Explanation => "explanation";
   public string Mnemonic => "mnemonic";
   public string[] Forms { get; } = ["form"];
   public string[] PartsOfSpeech { get; } = ["noun"];
   public string[] CompoundParts { get; } = ["part"];
   public string[] Synonyms { get; } = ["synonym"];
   public string[] SeeAlso { get; } = ["see also"];
   public string[] Antonyms { get; } = ["antonym"];
   public string[] ConfusedWith { get; } = ["confused with"];
   public string[] PerfectSynonyms { get; } = ["perfect synonym"];

   public VocabTevent.Loaded Loaded() => new(new TaggregateId(Id), Question, Answer, Explanation, Mnemonic, Forms, PartsOfSpeech, CompoundParts, Synonyms);
}
