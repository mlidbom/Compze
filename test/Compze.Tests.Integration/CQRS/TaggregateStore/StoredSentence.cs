using Compze.Abstractions;
using Compze.Teventive.TaggregateStore;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Tests.Integration.CQRS.TaggregateStore;
#pragma warning disable CA1812 //Uninstantiated class (used via reflection)

///<summary>A sentence note, much reduced: a second type of taggregate, for a unit of work that holds one type and looks up another.</summary>
class StoredSentence : Taggregate<StoredSentence, IStoredSentenceTevent, StoredSentenceTevent, IStoredSentenceTevent<IStoredSentenceTevent>, StoredSentenceTevent<StoredSentenceTevent>>, ISnapshotTaggregate
{
   public string Text { get; private set; } = "";

   public StoredSentence() => RegisterTeventAppliers().For<IStoredSentenceCreated>(it => Text = it.Text);

   public static StoredSentence Create(TaggregateId id, string text)
   {
      var sentence = new StoredSentence();
      sentence.Publish(new StoredSentenceCreated(id, text));
      return sentence;
   }

   public ITaggregateSnapshot TakeSnapshot() => new StoredSentenceSnapshot(Text);

   internal static StoredSentence RestoredFrom(StoredSentenceSnapshot snapshot, TaggregateId id, int version)
   {
      var sentence = new StoredSentence();
      sentence.RestoreIdentity(id, version);
      sentence.Text = snapshot.Text;
      return sentence;
   }
}

class StoredSentenceSnapshot(string text) : ITaggregateSnapshot
{
   public string Text { get; } = text;

   public ISnapshotTaggregate Restore(TaggregateId id, int version) => StoredSentence.RestoredFrom(this, id, version);
}

interface IStoredSentenceTevent<out T> : ITaggregateTevent<T> where T : IStoredSentenceTevent;
interface IStoredSentenceTevent : ITaggregateTevent;

class StoredSentenceTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IStoredSentenceTevent<T> where T : IStoredSentenceTevent;
abstract class StoredSentenceTevent : TaggregateTevent, IStoredSentenceTevent
{
   protected StoredSentenceTevent() {}
   protected StoredSentenceTevent(TaggregateId taggregateId) : base(taggregateId) {}
}

interface IStoredSentenceCreated : IStoredSentenceTevent, ITaggregateCreatedTevent
{
   string Text { get; }
}

class StoredSentenceCreated(TaggregateId id, string text) : StoredSentenceTevent(id), IStoredSentenceCreated
{
   public string Text { get; private set; } = text;
}
