using Compze.Abstractions;
using Compze.Teventive.TaggregateStore;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Tests.Integration.CQRS.TaggregateStore;
#pragma warning disable CA1812 //Uninstantiated class (used via reflection)

///<summary>A vocab note, much reduced: a taggregate that a taggregate store keeps as its state.</summary>
class StoredVocab : Taggregate<StoredVocab, IStoredVocabTevent, StoredVocabTevent, IStoredVocabTevent<IStoredVocabTevent>, StoredVocabTevent<StoredVocabTevent>>, ISnapshotTaggregate
{
   public const string MeaningThatFailsToApply = "a meaning that fails to apply";

   public string Question { get; private set; } = "";
   public string Meaning { get; private set; } = "";

   public StoredVocab()
   {
      RegisterTeventAppliers()
        .For<IStoredVocabCreated>(it => Question = it.Question)
        .For<IStoredVocabQuestionChanged>(it => Question = it.Question)
        .For<IStoredVocabMeaningChanged>(it =>
         {
            if(it.Meaning == MeaningThatFailsToApply) throw new InvalidOperationException("This meaning fails to apply.");
            Meaning = it.Meaning;
         });
   }

   public static StoredVocab Create(TaggregateId id, string question)
   {
      var vocab = new StoredVocab();
      vocab.Publish(new StoredVocabCreated(id, question));
      return vocab;
   }

   public void ChangeMeaning(string meaning) => Publish(new StoredVocabMeaningChanged(meaning));

   public void ChangeQuestion(string question) => Publish(new StoredVocabQuestionChanged(question));

   public ITaggregateSnapshot TakeSnapshot() => new StoredVocabSnapshot(Question, Meaning);

   internal static StoredVocab RestoredFrom(StoredVocabSnapshot snapshot, TaggregateId id, int version)
   {
      var vocab = new StoredVocab();
      vocab.RestoreIdentity(id, version);
      vocab.Question = snapshot.Question;
      vocab.Meaning = snapshot.Meaning;
      return vocab;
   }
}

class StoredVocabSnapshot(string question, string meaning) : ITaggregateSnapshot
{
   public string Question { get; } = question;
   public string Meaning { get; } = meaning;

   public ISnapshotTaggregate Restore(TaggregateId id, int version) => StoredVocab.RestoredFrom(this, id, version);
}

interface IStoredVocabTevent<out T> : ITaggregateTevent<T> where T : IStoredVocabTevent;
interface IStoredVocabTevent : ITaggregateTevent;

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
