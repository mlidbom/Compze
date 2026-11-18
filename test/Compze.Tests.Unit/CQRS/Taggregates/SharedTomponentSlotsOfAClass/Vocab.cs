using Compze.Abstractions;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses.Shared;
using Compze.Teventive.Taggregates.Tevents;

// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CA1812 // Avoid uninstantiated internal classes # used via reflection
#pragma warning disable CA1715 // Nested tevent interfaces follow the semantic events naming convention

namespace Compze.Tests.Unit.CQRS.Taggregates.SharedTomponentSlotsOfAClass;

interface IVocabTevent : ITaggregateTevent
{
   interface QuestionUpdated : IVocabTevent { string Question { get; } }
   interface Created : ITaggregateCreatedTevent, QuestionUpdated;

   ///<summary>A meaning of the vocab's own that one slot's adopting wrapper tevent has, for subscribers that follow the meaning and not the slot.</summary>
   interface ChangeToWhatItMatches : IVocabTevent;

   ///<summary>The tevents of the vocab's component, which holds two shared tomponents of one class.</summary>
   interface Related : IVocabTevent;
}

interface IVocabTevent<out T> : ITaggregateTevent<T> where T : IVocabTevent;
class VocabTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IVocabTevent<T> where T : IVocabTevent;

class VocabTevent : TaggregateTevent, IVocabTevent
{
   protected VocabTevent() {}
   VocabTevent(TaggregateId taggregateId) : base(taggregateId) {}

   internal class Created(TaggregateId taggregateId, string question) : VocabTevent(taggregateId), IVocabTevent.Created { public string Question { get; } = question; }
   internal abstract class Related : VocabTevent, IVocabTevent.Related;
}

///<summary>A shared tomponent: its tevents are bare <see cref="ITevent"/>s of its own hierarchy.</summary>
interface IWordSetTevent : ITevent
{
   interface Added : IWordSetTevent { string Word { get; } }
}

class WordSetTevent : IWordSetTevent
{
   internal class Added(string word) : WordSetTevent, IWordSetTevent.Added { public string Word { get; } = word; }
}

///<summary>The adopting wrapper tevent of the synonyms' slot, in the vocab's component. It has a meaning of the vocab's own too.</summary>
interface ISynonymsTevent<out T> : IVocabTevent.Related, IVocabTevent.ChangeToWhatItMatches, IPublisherTevent<T> where T : IWordSetTevent;
class SynonymsTevent<T>(T tevent) : VocabTevent.Related, ISynonymsTevent<T> where T : IWordSetTevent { public T Tevent { get; } = tevent; }

///<summary>The adopting wrapper tevent of the antonyms' slot, in the same component: a second shared tomponent of the same class.</summary>
interface IAntonymsTevent<out T> : IVocabTevent.Related, IPublisherTevent<T> where T : IWordSetTevent;
class AntonymsTevent<T>(T tevent) : VocabTevent.Related, IAntonymsTevent<T> where T : IWordSetTevent { public T Tevent { get; } = tevent; }

///<summary>The adopting wrapper tevent of the see-also words' slot, in the vocab itself.</summary>
interface ISeeAlsoTevent<out T> : IVocabTevent, IPublisherTevent<T> where T : IWordSetTevent;
class SeeAlsoTevent<T>(T tevent) : VocabTevent, ISeeAlsoTevent<T> where T : IWordSetTevent { public T Tevent { get; } = tevent; }

///<summary>A taggregate whose shared tomponents are in slots that belong to its classes: one slot in the vocab itself, and two of one class in its<br/>
/// component, as most of the shared tomponents of JAStudio's notes are. Each class declares its slots in a nested static class.</summary>
class Vocab : Taggregate<Vocab, IVocabTevent, VocabTevent, IVocabTevent<IVocabTevent>, VocabTevent<VocabTevent>>
{
   static class Slots
   {
      internal static readonly SharedTomponentSlotOfAClass<Vocab, IVocabTevent, VocabTevent, IWordSetTevent, ISeeAlsoTevent<IWordSetTevent>> SeeAlso =
         new(typeof(SeeAlsoTevent<>), static vocab => vocab.SeeAlso);
   }

   static readonly TeventRouting<Vocab, IVocabTevent> Appliers = DeclareAppliers(it => it
     .For<IVocabTevent.QuestionUpdated>(static (vocab, tevent) => vocab.Question = tevent.Question)
     .Component(static vocab => vocab.Related)
     .SharedTomponent(Slots.SeeAlso));

   protected override TeventRouting TeventAppliers => Appliers;

   Vocab()
   {
      Related = new RelatedVocab(this);
      SeeAlso = new WordSet(Slots.SeeAlso.In(this));
   }

   public string Question { get; private set; } = "";
   public RelatedVocab Related { get; }
   public WordSet SeeAlso { get; }

   public static Vocab Create(string question)
   {
      var vocab = new Vocab();
      vocab.Publish(new VocabTevent.Created(new TaggregateId(), question));
      return vocab;
   }

   public static Vocab LoadFromHistory(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history)
   {
      var vocab = new Vocab();
      ((ITaggregate)vocab).LoadFromHistory(history);
      return vocab;
   }
}

///<summary>A component with two shared tomponents of one class, each in a slot that belongs to the component's class. Its routing is<br/>
/// declared above its slots, which the nested class makes fine.</summary>
class RelatedVocab : TeventiveComponent<Vocab, IVocabTevent, VocabTevent, RelatedVocab, IVocabTevent.Related, VocabTevent.Related>
{
   static readonly TeventRouting<RelatedVocab, IVocabTevent.Related> Appliers = DeclareAppliers(it => it
     .SharedTomponent(Slots.Synonyms)
     .SharedTomponent(Slots.Antonyms));

   static class Slots
   {
      internal static readonly SharedTomponentSlotOfAClass<RelatedVocab, IVocabTevent.Related, VocabTevent.Related, IWordSetTevent, ISynonymsTevent<IWordSetTevent>> Synonyms =
         new(typeof(SynonymsTevent<>), static related => related.Synonyms);

      internal static readonly SharedTomponentSlotOfAClass<RelatedVocab, IVocabTevent.Related, VocabTevent.Related, IWordSetTevent, IAntonymsTevent<IWordSetTevent>> Antonyms =
         new(typeof(AntonymsTevent<>), static related => related.Antonyms);
   }

   protected override TeventRouting TeventAppliers => Appliers;

   public RelatedVocab(Vocab vocab) : base(vocab)
   {
      Synonyms = new WordSet(Slots.Synonyms.In(this));
      Antonyms = new WordSet(Slots.Antonyms.In(this));
   }

   public WordSet Synonyms { get; }
   public WordSet Antonyms { get; }
}

///<summary>A shared tomponent that is constructed with a slot of its owner's class and that owner.</summary>
class WordSet : SharedTomponent<IWordSetTevent>
{
   static readonly TeventRouting<WordSet, IWordSetTevent> Appliers = DeclareAppliers<WordSet>(it => it
     .For<IWordSetTevent.Added>(static (words, tevent) => words._words.Add(tevent.Word)));

   protected override TeventRouting TeventAppliers => Appliers;

   readonly List<string> _words = [];

   public WordSet(SharedTomponentSlotInAnOwner<IWordSetTevent> slot) : base(slot) {}

   public IReadOnlyList<string> Words => _words;

   public void Add(string word) => Publish(new WordSetTevent.Added(word));
}
