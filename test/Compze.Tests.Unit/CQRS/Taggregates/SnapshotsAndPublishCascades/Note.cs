using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Threading.Testing;
using JetBrains.Annotations;

// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CA1812 // Avoid uninstantiated internal classes # used via reflection
#pragma warning disable CA1715 // Nested tevent interfaces follow the semantic events naming convention (compze.net/paradigms/semantic-events/event-naming.html)

namespace Compze.Tests.Unit.CQRS.Taggregates.SnapshotsAndPublishCascades;

interface INoteTevent : ITaggregateTevent
{
   interface Created : INoteTevent, ITaggregateCreatedTevent, TextUpdated;

   ///<summary>The note's state as stored: the tevent a snapshot of a note starts with. It updates the text, so the text's applier restores it.</summary>
   interface Snapshot : INoteTevent, ITaggregateSnapshotTevent, TextUpdated;

   interface TextUpdated : INoteTevent
   {
      string Text { get; }
   }

   interface Edited : TextUpdated;

   ///<summary>Published in reaction to another tevent: by a tevent handler, an observer, the per-cascade hook or a commit callback.</summary>
   interface Reacted : INoteTevent;

   interface Sense : INoteTevent
   {
      Guid SenseId { get; }

      new interface Created : Sense, MeaningUpdated;

      interface MeaningUpdated : Sense
      {
         string Meaning { get; }
      }
   }
}

interface INoteTevent<out T> : ITaggregateTevent<T> where T : INoteTevent;

class NoteTevent<T>(T tevent) : TaggregateTevent<T>(tevent), INoteTevent<T> where T : INoteTevent;

abstract class NoteTevent : TaggregateTevent, INoteTevent
{
   protected NoteTevent() {}
   NoteTevent(TaggregateId taggregateId) : base(taggregateId) {}

   internal class Created(TaggregateId taggregateId, string text) : NoteTevent(taggregateId), INoteTevent.Created
   {
      public string Text { get; } = text;
   }

   internal class Snapshot(TaggregateId taggregateId, string text) : NoteTevent(taggregateId), INoteTevent.Snapshot
   {
      public string Text { get; } = text;
   }

   internal class Edited(string text) : NoteTevent, INoteTevent.Edited
   {
      public string Text { get; } = text;
   }

   internal class Reacted : NoteTevent, INoteTevent.Reacted;

   internal abstract class Sense : NoteTevent, INoteTevent.Sense
   {
      public Guid SenseId { get; private set; }

      [UsedImplicitly] internal class IdGetterSetter : IGetSetTaggregateEntityTeventEntityId<Guid, Sense, INoteTevent.Sense>
      {
         public void SetEntityId(Sense tevent, Guid id) => tevent.SenseId = id;
         public Guid GetId(INoteTevent.Sense tevent) => tevent.SenseId;
      }

      internal new class Created : Sense, INoteTevent.Sense.Created
      {
         public Created(Guid senseId, string meaning)
         {
            SenseId = senseId;
            Meaning = meaning;
         }

         public string Meaning { get; }
      }
   }
}

///<summary>A taggregate for the specifications of snapshots and publish cascades: a text, senses as an entity collection, a tevent handler that can react<br/>
/// to an edit by publishing, and an override of <see cref="OnPublishCascadeEnded"/> that the specifications can act from.</summary>
class Note : Taggregate<Note, INoteTevent, NoteTevent, INoteTevent<INoteTevent>, NoteTevent<NoteTevent>>
{
   readonly NoteSense.ICollectionManager _senses;
   readonly List<INoteTevent> _teventsHandled = [];
   readonly List<IReadOnlyList<INoteTevent>> _publishCascadesEnded = [];
   bool _reactsToEditsInATeventHandler;
   IThreadGate? _gateEditsWaitAt;
   Action<Note>? _whenAPublishCascadeEnds;

   public Note()
   {
      _senses = NoteSense.CreateSelfManagingCollection(this);

      RegisterTeventAppliers()
        .For<INoteTevent.TextUpdated>(it => Text = it.Text)
        .For<INoteTevent.Reacted>(_ => {});

      RegisterTeventHandlers()
        .For<INoteTevent>(_teventsHandled.Add)
        .For<INoteTevent.Edited>(_ =>
         {
            _gateEditsWaitAt?.AwaitPassThrough();
            if(_reactsToEditsInATeventHandler) Publish(new NoteTevent.Reacted());
         });
   }

   public string Text { get; private set; } = "";
   public IReadOnlyEntityCollection<NoteSense, Guid> Senses => _senses.Entities;

   ///<summary>Every tevent the tevent handlers were called with, in order.</summary>
   public IReadOnlyList<INoteTevent> TeventsHandled => _teventsHandled;

   ///<summary>The tevents of each publish cascade <see cref="OnPublishCascadeEnded"/> was called with, in order.</summary>
   public IReadOnlyList<IReadOnlyList<INoteTevent>> PublishCascadesEnded => _publishCascadesEnded;

   public static Note Create(string text)
   {
      var note = new Note();
      note.Publish(new NoteTevent.Created(new TaggregateId(), text));
      return note;
   }

   public static Note LoadFromSnapshot(IEnumerable<ITaggregateTevent> snapshot, int version)
   {
      var note = new Note();
      ((ITaggregate)note).LoadFromSnapshot(snapshot, version);
      return note;
   }

   public void Edit(string text) => Publish(new NoteTevent.Edited(text));
   public void React() => Publish(new NoteTevent.Reacted());
   public NoteSense AddSense(string meaning) => _senses.AddByPublishing(new NoteTevent.Sense.Created(Guid.NewGuid(), meaning));

   ///<summary>From now on a tevent handler reacts to each edit by publishing a <see cref="INoteTevent.Reacted"/>, inside the edit's publish cascade.</summary>
   public void ReactToEditsInATeventHandler() => _reactsToEditsInATeventHandler = true;

   ///<summary>From now on each edit waits at <paramref name="gate"/> in a tevent handler, inside its publish cascade.</summary>
   public void MakeEditsWaitAt(IThreadGate gate) => _gateEditsWaitAt = gate;

   ///<summary>From now on <paramref name="action"/> is run at the end of each publish cascade, after the cascade has been recorded.</summary>
   public void WhenAPublishCascadeEnds(Action<Note> action) => _whenAPublishCascadeEnds = action;

   protected override void OnPublishCascadeEnded(IReadOnlyList<ITaggregateTevent<INoteTevent>> teventsOfTheCascade)
   {
      _publishCascadesEnded.Add([..teventsOfTheCascade.Select(it => it.Tevent)]);
      base.OnPublishCascadeEnded(teventsOfTheCascade);
      _whenAPublishCascadeEnds?.Invoke(this);
   }
}

[UsedImplicitly]
class NoteSense : Note.Entity<NoteSense, Guid, INoteTevent.Sense, NoteTevent.Sense, INoteTevent.Sense.Created, NoteTevent.Sense.IdGetterSetter>
{
   public NoteSense(Note note) : base(note) =>
      RegisterTeventAppliers().For<INoteTevent.Sense.MeaningUpdated>(it => Meaning = it.Meaning);

   public string Meaning { get; private set; } = "";
}
