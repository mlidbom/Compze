using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;
using JetBrains.Annotations;

namespace Compze.Tests.Integration.CQRS.StateStore;
#pragma warning disable CA1812 //Uninstantiated class (used via reflection)

///<summary>A sentence note, much reduced: a second taggregate class, for a unit of work that holds one class and looks up another.</summary>
class StoredSentence : Taggregate<StoredSentence, IStoredSentenceTevent, StoredSentenceTevent, IStoredSentenceTevent<IStoredSentenceTevent>, StoredSentenceTevent<StoredSentenceTevent>>,
                       ISnapshotable<StoredSentence, StoredSentenceSnapshot>, ITeventRouted
{
   static readonly TeventRouting<StoredSentence, IStoredSentenceTevent> Appliers = DeclareAppliers(it => it
     .For<IStoredSentenceCreated>(static (sentence, tevent) => sentence.Text = tevent.Text));

   TeventRouting ITeventRouted.TeventAppliers => Appliers;

   string Text { get; set; } = "";


   public static StoredSentence Create(TaggregateId id, string text)
   {
      var sentence = new StoredSentence();
      sentence.Publish(new StoredSentenceCreated(id, text));
      return sentence;
   }

   public StoredSentenceSnapshot TakeSnapshot() => new(Id, Text);

   public void RestoreFrom(StoredSentenceSnapshot snapshot)
   {
      RestoreIdentity(snapshot.Id);
      Text = snapshot.Text;
   }
}

class StoredSentenceSnapshot(TaggregateId id, string text)
{
   public TaggregateId Id { get; } = id;
   public string Text { get; } = text;
}

interface IStoredSentenceTevent<out T> : ITaggregateTevent<T> where T : IStoredSentenceTevent;
interface IStoredSentenceTevent : ITaggregateTevent;

[UsedImplicitly] //Built by reflection, around each tevent the taggregate publishes.
class StoredSentenceTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IStoredSentenceTevent<T> where T : IStoredSentenceTevent;
abstract class StoredSentenceTevent : TaggregateTevent, IStoredSentenceTevent
{
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
