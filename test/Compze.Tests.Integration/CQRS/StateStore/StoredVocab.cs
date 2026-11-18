using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;
using JetBrains.Annotations;

namespace Compze.Tests.Integration.CQRS.StateStore;
#pragma warning disable CA1812 //Uninstantiated class (used via reflection)

///<summary>A vocab note, much reduced: a taggregate that a state store keeps as its state.</summary>
class StoredVocab : Taggregate<StoredVocab, IStoredVocabTevent, StoredVocabTevent, IStoredVocabTevent<IStoredVocabTevent>, StoredVocabTevent<StoredVocabTevent>>,
                    ISnapshotable<StoredVocab, StoredVocabSnapshot>
{
   static readonly TeventRouting<StoredVocab, IStoredVocabTevent> Appliers = DeclareAppliers(it => it
     .For<IStoredVocabCreated>(static (vocab, tevent) => vocab.Question = tevent.Question)
     .For<IStoredVocabQuestionChanged>(static (vocab, tevent) => vocab.Question = tevent.Question)
     .For<IStoredVocabMeaningChanged>(static (vocab, tevent) =>
      {
         if(tevent.Meaning == MeaningThatFailsToApply) throw new InvalidOperationException("This meaning fails to apply.");
         vocab.Meaning = tevent.Meaning;
      })
     .For<IStoredVocabDeleted>(static (_, _) => {}));

   protected override TeventRouting TeventAppliers => Appliers;

   public const string MeaningThatFailsToApply = "a meaning that fails to apply";

   public string Question { get; private set; } = "";
   public string Meaning { get; private set; } = "";

   ///<summary>The dictionary the container built this vocab with, if it built it: for the specifications of taggregates with dependencies.</summary>
   public VocabDictionary? Dictionary { get; }

   ///<summary>The readings the container built this vocab with, if it built it.</summary>
   public VocabReadings? Readings { get; }

   ///<summary>A vocab with nothing of the container in it, as the domain makes a new one.</summary>
   public StoredVocab() {}

   ///<summary>A vocab built with two services of the container, as a state store declared with a registration builds one.</summary>
   public StoredVocab(VocabDictionary dictionary, VocabReadings readings)
   {
      Dictionary = dictionary;
      Readings = readings;
   }


   public static StoredVocab Create(TaggregateId id, string question)
   {
      var vocab = new StoredVocab();
      vocab.Publish(new StoredVocabCreated(id, question));
      return vocab;
   }

   public void ChangeMeaning(string meaning) => Publish(new StoredVocabMeaningChanged(meaning));

   public void ChangeQuestion(string question) => Publish(new StoredVocabQuestionChanged(question));

   public void Delete() => Publish(new StoredVocabDeleted());

   public StoredVocabSnapshot TakeSnapshot() => new(Id, Question, Meaning);

   public void RestoreFrom(StoredVocabSnapshot snapshot)
   {
      RestoreIdentity(snapshot.Id);
      Question = snapshot.Question;
      Meaning = snapshot.Meaning;
   }
}

///<summary>A vocab's whole state, as a state store's storage keeps it. It doesn't override Equals, which a reload doesn't need.</summary>
class StoredVocabSnapshot(TaggregateId id, string question, string meaning)
{
   public TaggregateId Id { get; } = id;
   public string Question { get; } = question;
   public string Meaning { get; } = meaning;
}

interface IStoredVocabTevent<out T> : ITaggregateTevent<T> where T : IStoredVocabTevent;
interface IStoredVocabTevent : ITaggregateTevent;

[UsedImplicitly] //Built by reflection, around each tevent the taggregate publishes.
class StoredVocabTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IStoredVocabTevent<T> where T : IStoredVocabTevent;
abstract class StoredVocabTevent : TaggregateTevent, IStoredVocabTevent
{
   protected StoredVocabTevent() {}
   protected StoredVocabTevent(TaggregateId taggregateId) : base(taggregateId) {}
}

interface IStoredVocabCreated : IStoredVocabTevent, ITaggregateCreatedTevent
{
   string Question { get; }
}

class StoredVocabCreated(TaggregateId id, string question) : StoredVocabTevent(id), IStoredVocabCreated
{
   public string Question { get; private set; } = question;
}

interface IStoredVocabQuestionChanged : IStoredVocabTevent
{
   string Question { get; }
}

class StoredVocabQuestionChanged(string question) : StoredVocabTevent, IStoredVocabQuestionChanged
{
   public string Question { get; private set; } = question;
}

interface IStoredVocabMeaningChanged : IStoredVocabTevent
{
   string Meaning { get; }
}

class StoredVocabMeaningChanged(string meaning) : StoredVocabTevent, IStoredVocabMeaningChanged
{
   public string Meaning { get; private set; } = meaning;
}

interface IStoredVocabDeleted : IStoredVocabTevent, ITaggregateDeletedTevent;

class StoredVocabDeleted : StoredVocabTevent, IStoredVocabDeleted;
