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
   interface Snapshot : ITaggregateSnapshotTevent, QuestionUpdated;

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
   internal class Snapshot(TaggregateId taggregateId, string question) : VocabTevent(taggregateId), IVocabTevent.Snapshot { public string Question { get; } = question; }
   internal abstract class Related : VocabTevent, IVocabTevent.Related;
}

///<summary>A shared tomponent: its tevents are bare <see cref="ITevent"/>s of its own hierarchy.</summary>
interface IWordSetTevent : ITevent
{
   interface Added : IWordSetTevent { string Word { get; } }
   interface Replaced : IWordSetTevent { IReadOnlyList<string> Words { get; } }
}

class WordSetTevent : IWordSetTevent
{
   internal class Added(string word) : WordSetTevent, IWordSetTevent.Added { public string Word { get; } = word; }
   internal class Replaced(IReadOnlyList<string> words) : WordSetTevent, IWordSetTevent.Replaced { public IReadOnlyList<string> Words { get; } = words; }
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

///<summary>A taggregate whose shared tomponents are in slots that belong to its classes: one slot in the vocab itself, and two of one class in its component,<br/>
/// as most of the shared tomponents of JAStudio's notes are.</summary>
class Vocab : Taggregate<Vocab, IVocabTevent, VocabTevent, IVocabTevent<IVocabTevent>, VocabTevent<VocabTevent>>
{
   //The slot is declared above the routing that enters it: static fields are initialized in the order written.
   static readonly SharedTomponentSlotOfAClass<Vocab, IVocabTevent, VocabTevent, IWordSetTevent, ISeeAlsoTevent<IWordSetTevent>> SeeAlsoSlot =
      new(typeof(SeeAlsoTevent<>), static vocab => vocab.SeeAlso);

   static readonly TeventRouting<Vocab> VocabAppliers = TeventRouting<Vocab>.DeclareAppliers(it => it
     .For<IVocabTevent.QuestionUpdated>(static (vocab, tevent) => vocab.Question = tevent.Question)
     .Component(static vocab => vocab.Related)
     .SharedTomponent(SeeAlsoSlot));

   protected override TeventRouting TeventAppliers => VocabAppliers;

   Vocab()
   {
      Related = new RelatedVocab(this);
      SeeAlso = new WordSet(SeeAlsoSlot.In(this));
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

   ///<summary>Each part gives its share of the snapshot from its stored state, and the slots make the adopted tevents.</summary>
   public static Vocab LoadFromSnapshot(TaggregateId id, string question, IReadOnlyList<string> seeAlso, IReadOnlyList<string> synonyms, IReadOnlyList<string> antonyms)
   {
      var vocab = new Vocab();
      ((ITaggregate)vocab).LoadFromSnapshot([
                                               new VocabTevent.Snapshot(id, question),
                                               SeeAlsoSlot.Adopt(new WordSetTevent.Replaced(seeAlso)),
                                               ..RelatedVocab.SnapshotOf(synonyms, antonyms)
                                            ],
                                            version: 1);
      return vocab;
   }

   public static Vocab LoadFromHistory(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history)
   {
      var vocab = new Vocab();
      ((ITaggregate)vocab).LoadFromHistory(history);
      return vocab;
   }
}

///<summary>A component with two shared tomponents of one class, each in a slot that belongs to the component's class.</summary>
class RelatedVocab : TeventiveComponent<Vocab, IVocabTevent, VocabTevent, RelatedVocab, IVocabTevent.Related, VocabTevent.Related>
{
   static readonly SharedTomponentSlotOfAClass<RelatedVocab, IVocabTevent.Related, VocabTevent.Related, IWordSetTevent, ISynonymsTevent<IWordSetTevent>> SynonymsSlot =
      new(typeof(SynonymsTevent<>), static related => related.Synonyms);

   static readonly SharedTomponentSlotOfAClass<RelatedVocab, IVocabTevent.Related, VocabTevent.Related, IWordSetTevent, IAntonymsTevent<IWordSetTevent>> AntonymsSlot =
      new(typeof(AntonymsTevent<>), static related => related.Antonyms);

   static readonly TeventRouting<RelatedVocab> RelatedVocabAppliers = TeventRouting<RelatedVocab>.DeclareAppliers(it => it
     .SharedTomponent(SynonymsSlot)
     .SharedTomponent(AntonymsSlot));

   protected override TeventRouting TeventAppliers => RelatedVocabAppliers;

   public RelatedVocab(Vocab vocab) : base(vocab)
   {
      Synonyms = new WordSet(SynonymsSlot.In(this));
      Antonyms = new WordSet(AntonymsSlot.In(this));
   }

   public WordSet Synonyms { get; }
   public WordSet Antonyms { get; }

   internal static IEnumerable<VocabTevent> SnapshotOf(IReadOnlyList<string> synonyms, IReadOnlyList<string> antonyms) =>
   [
      SynonymsSlot.Adopt(new WordSetTevent.Replaced(synonyms)),
      AntonymsSlot.Adopt(new WordSetTevent.Replaced(antonyms))
   ];
}

///<summary>A shared tomponent that is constructed with a slot of its owner's class and that owner.</summary>
class WordSet : SharedTomponent<IWordSetTevent>
{
   static readonly TeventRouting<WordSet> WordSetAppliers = TeventRouting<WordSet>.DeclareAppliers(it => it
     .For<IWordSetTevent.Added>(static (words, tevent) => words._words.Add(tevent.Word))
     .For<IWordSetTevent.Replaced>(static (words, tevent) =>
      {
         words._words.Clear();
         words._words.AddRange(tevent.Words);
      }));

   protected override TeventRouting TeventAppliers => WordSetAppliers;

   readonly List<string> _words = [];

   public WordSet(SharedTomponentSlotInAnOwner<IWordSetTevent> slot) : base(slot) {}

   public IReadOnlyList<string> Words => _words;

   public void Add(string word) => Publish(new WordSetTevent.Added(word));
}

///<summary>A component whose slot is declared below the routing that enters it, which is a mistake: the slot is null where the routing is declared.</summary>
class ComponentWithItsSlotDeclaredBelowItsRouting : TeventiveComponent<Vocab, IVocabTevent, VocabTevent, ComponentWithItsSlotDeclaredBelowItsRouting, IVocabTevent.Related, VocabTevent.Related>
{
#pragma warning disable CS8604 // The compiler warns of exactly the mistake this class is here to make.
   internal static readonly TeventRouting<ComponentWithItsSlotDeclaredBelowItsRouting> Appliers = TeventRouting<ComponentWithItsSlotDeclaredBelowItsRouting>.DeclareAppliers(it => it
     .SharedTomponent(SynonymsSlot));
#pragma warning restore CS8604

   static readonly SharedTomponentSlotOfAClass<ComponentWithItsSlotDeclaredBelowItsRouting, IVocabTevent.Related, VocabTevent.Related, IWordSetTevent, ISynonymsTevent<IWordSetTevent>> SynonymsSlot =
      new(typeof(SynonymsTevent<>), static component => component.Synonyms);

   protected override TeventRouting TeventAppliers => Appliers;

   ComponentWithItsSlotDeclaredBelowItsRouting(Vocab vocab) : base(vocab) => Synonyms = new WordSet(SynonymsSlot.In(this));

   public WordSet Synonyms { get; }
}
