using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;
using JetBrains.Annotations;
using MemoryPack;

namespace Compze.Tests.Integration.CQRS.StateStore;
#pragma warning disable CA1812 //Uninstantiated class (used via reflection)

///<summary>A vocab note, much reduced, whose snapshot MemoryPack serializes: for a state store kept in files that starts from a<br/>
/// MemoryPack cache.</summary>
class CachedVocab : Taggregate<CachedVocab, ICachedVocabTevent, CachedVocabTevent, ICachedVocabTevent<ICachedVocabTevent>, CachedVocabTevent<CachedVocabTevent>>,
                    ISnapshotable<CachedVocab, CachedVocabSnapshot>, ITeventRouted
{
   static readonly TeventRouting<CachedVocab, ICachedVocabTevent> Appliers = DeclareAppliers(it => it
     .For<ICachedVocabCreated>(static (vocab, tevent) => vocab.Question = tevent.Question)
     .For<ICachedVocabMeaningChanged>(static (vocab, tevent) => vocab.Meaning = tevent.Meaning)
     .For<ICachedVocabDeleted>(static (_, _) => {}));

   TeventRouting ITeventRouted.TeventAppliers => Appliers;

   string Question { get; set; } = "";
   public string Meaning { get; private set; } = "";

   public static CachedVocab Create(TaggregateId id, string question, string meaning)
   {
      var vocab = new CachedVocab();
      vocab.Publish(new CachedVocabCreated(id, question));
      vocab.ChangeMeaning(meaning);
      return vocab;
   }

   void ChangeMeaning(string meaning) => Publish(new CachedVocabMeaningChanged(meaning));

   public void Delete() => Publish(new CachedVocabDeleted());

   public CachedVocabSnapshot TakeSnapshot() => new() { Id = Id.Value, Question = Question, Meaning = Meaning };

   public void RestoreFrom(CachedVocabSnapshot snapshot)
   {
      RestoreIdentity(new TaggregateId(snapshot.Id));
      Question = snapshot.Question;
      Meaning = snapshot.Meaning;
   }
}

///<summary>A <see cref="CachedVocab"/>'s whole state, as its JSON file and the MemoryPack cache hold it.</summary>
[MemoryPackable]
partial class CachedVocabSnapshot : IEquatable<CachedVocabSnapshot>
{
   public Guid Id { get; init; }
   public string Question { get; init; } = "";
   public string Meaning { get; init; } = "";

   public bool Equals(CachedVocabSnapshot? other) => other != null && other.Id == Id && other.Question == Question && other.Meaning == Meaning;
   public override bool Equals(object? other) => Equals(other as CachedVocabSnapshot);
   public override int GetHashCode() => HashCode.Combine(Id, Question, Meaning);
}

///<summary>The same vocab note, as a later version of the program keeps it: its snapshot has one field more, so a cache written for<br/>
/// <see cref="CachedVocabSnapshot"/> doesn't fit it, while the JSON files still do.</summary>
class CachedVocabWithTags : Taggregate<CachedVocabWithTags, ICachedVocabTevent, CachedVocabTevent, ICachedVocabTevent<ICachedVocabTevent>, CachedVocabTevent<CachedVocabTevent>>,
                            ISnapshotable<CachedVocabWithTags, CachedVocabWithTagsSnapshot>, ITeventRouted
{
   static readonly TeventRouting<CachedVocabWithTags, ICachedVocabTevent> Appliers = DeclareAppliers(it => it
     .For<ICachedVocabCreated>(static (vocab, tevent) => vocab.Question = tevent.Question)
     .For<ICachedVocabMeaningChanged>(static (vocab, tevent) => vocab.Meaning = tevent.Meaning)
     .For<ICachedVocabDeleted>(static (_, _) => {}));

   TeventRouting ITeventRouted.TeventAppliers => Appliers;

   string Question { get; set; } = "";
   public string Meaning { get; private set; } = "";
   IReadOnlyList<string> Tags { get; set; } = [];

   public CachedVocabWithTagsSnapshot TakeSnapshot() => new() { Id = Id.Value, Question = Question, Meaning = Meaning, Tags = [..Tags] };

   public void RestoreFrom(CachedVocabWithTagsSnapshot snapshot)
   {
      RestoreIdentity(new TaggregateId(snapshot.Id));
      Question = snapshot.Question;
      Meaning = snapshot.Meaning;
      Tags = snapshot.Tags;
   }
}

///<summary>A <see cref="CachedVocabWithTags"/>'s whole state: a <see cref="CachedVocabSnapshot"/> with tags.</summary>
[MemoryPackable]
partial class CachedVocabWithTagsSnapshot : IEquatable<CachedVocabWithTagsSnapshot>
{
   public Guid Id { get; init; }
   public string Question { get; init; } = "";
   public string Meaning { get; init; } = "";
   public List<string> Tags { get; init; } = [];

   public bool Equals(CachedVocabWithTagsSnapshot? other) => other != null && other.Id == Id && other.Question == Question && other.Meaning == Meaning && other.Tags.SequenceEqual(Tags);
   public override bool Equals(object? other) => Equals(other as CachedVocabWithTagsSnapshot);
   public override int GetHashCode() => HashCode.Combine(Id, Question, Meaning);
}

interface ICachedVocabTevent<out T> : ITaggregateTevent<T> where T : ICachedVocabTevent;
interface ICachedVocabTevent : ITaggregateTevent;

[UsedImplicitly] //Built by reflection, around each tevent the taggregate publishes.
class CachedVocabTevent<T>(T tevent) : TaggregateTevent<T>(tevent), ICachedVocabTevent<T> where T : ICachedVocabTevent;
abstract class CachedVocabTevent : TaggregateTevent, ICachedVocabTevent
{
   protected CachedVocabTevent() {}
   protected CachedVocabTevent(TaggregateId taggregateId) : base(taggregateId) {}
}

interface ICachedVocabCreated : ICachedVocabTevent, ITaggregateCreatedTevent
{
   string Question { get; }
}

class CachedVocabCreated(TaggregateId id, string question) : CachedVocabTevent(id), ICachedVocabCreated
{
   public string Question { get; private set; } = question;
}

interface ICachedVocabMeaningChanged : ICachedVocabTevent
{
   string Meaning { get; }
}

class CachedVocabMeaningChanged(string meaning) : CachedVocabTevent, ICachedVocabMeaningChanged
{
   public string Meaning { get; private set; } = meaning;
}

interface ICachedVocabDeleted : ICachedVocabTevent, ITaggregateDeletedTevent;

class CachedVocabDeleted : CachedVocabTevent, ICachedVocabDeleted;
