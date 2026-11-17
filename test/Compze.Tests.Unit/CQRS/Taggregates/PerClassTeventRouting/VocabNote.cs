using Compze.Abstractions;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses.Shared;
using Compze.Teventive.Taggregates.Tevents;

// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CA1812 // Avoid uninstantiated internal classes # used via reflection
#pragma warning disable CA1715 // Nested tevent interfaces follow the semantic events naming convention

namespace Compze.Tests.Unit.CQRS.Taggregates.PerClassTeventRouting;

interface INoteTevent : ITaggregateTevent
{
   interface Created : INoteTevent, ITaggregateCreatedTevent;
   interface Renamed : INoteTevent { string Name { get; } }
   interface Archived : INoteTevent;
}

interface INoteTevent<out T> : ITaggregateTevent<T> where T : INoteTevent;
class NoteTevent<T>(T tevent) : TaggregateTevent<T>(tevent), INoteTevent<T> where T : INoteTevent;

class NoteTevent : TaggregateTevent, INoteTevent
{
   protected NoteTevent() {}
   NoteTevent(TaggregateId taggregateId) : base(taggregateId) {}

   internal class Created(TaggregateId taggregateId) : NoteTevent(taggregateId), INoteTevent.Created;
   internal class Renamed(string name) : NoteTevent, INoteTevent.Renamed { public string Name { get; } = name; }
   internal class Archived : NoteTevent, INoteTevent.Archived;
}

interface IVocabTevent : INoteTevent
{
   interface Forms : IVocabTevent
   {
      interface Updated : Forms { string AllForms { get; } }
      interface Counted : Forms { int Count { get; } }
      interface RoutedNowhere : Forms;
   }

   interface Sense : IVocabTevent
   {
      Guid SenseId { get; }
      interface Defined : Sense { string Gloss { get; } }
      interface Added : Defined;
      interface Redefined : Defined;
   }
}

interface IVocabTevent<out T> : INoteTevent<T> where T : INoteTevent;
class VocabTevent<T>(T tevent) : NoteTevent<T>(tevent), IVocabTevent<T> where T : INoteTevent;

class VocabTevent : NoteTevent, IVocabTevent
{
   internal abstract class Forms : VocabTevent, IVocabTevent.Forms
   {
      internal class Updated(string allForms) : Forms, IVocabTevent.Forms.Updated { public string AllForms { get; } = allForms; }
      internal class Counted(int count) : Forms, IVocabTevent.Forms.Counted { public int Count { get; } = count; }
      internal class RoutedNowhere : Forms, IVocabTevent.Forms.RoutedNowhere;
   }

   internal abstract class Sense : VocabTevent, IVocabTevent.Sense
   {
      public Guid SenseId { get; private set; }

      internal class IdGetterSetter : IGetSetTaggregateEntityTeventEntityId<Guid, Sense, IVocabTevent.Sense>
      {
         public void SetEntityId(Sense tevent, Guid id) => tevent.SenseId = id;
         public Guid GetId(IVocabTevent.Sense tevent) => tevent.SenseId;
      }

      internal class Added : Sense, IVocabTevent.Sense.Added
      {
         public Added(Guid senseId, string gloss)
         {
            SenseId = senseId;
            Gloss = gloss;
         }

         public string Gloss { get; }
      }

      internal class Redefined(string gloss) : Sense, IVocabTevent.Sense.Redefined { public string Gloss { get; } = gloss; }
   }
}

///<summary>A shared tomponent: its tevents are bare <see cref="ITevent"/>s of its own hierarchy.</summary>
interface IAudioListTevent : ITevent
{
   interface Added : IAudioListTevent { string File { get; } }
}

class AudioListTevent : IAudioListTevent
{
   internal class Added(string file) : AudioListTevent, IAudioListTevent.Added { public string File { get; } = file; }
}

///<summary>The adopting wrapper tevent of a vocab's audio-list slot.</summary>
interface IVocabAudioTevent<out T> : IVocabTevent, IPublisherTevent<T> where T : IAudioListTevent;
class VocabAudioTevent<T>(T tevent) : VocabTevent, IVocabAudioTevent<T> where T : IAudioListTevent { public T Tevent { get; } = tevent; }

///<summary>The root of an inheritance hierarchy of taggregates, as JAStudio's JPNote is: it closes the <see cref="Taggregate{TTaggregate,TTaggregateTevent,TTaggregateTeventImplementation,TWrapperTeventInterface,TWrapperTeventImplementation}"/> generic.</summary>
class Note : Taggregate<Note, INoteTevent, NoteTevent, INoteTevent<INoteTevent>, NoteTevent<NoteTevent>>, ITeventRouted
{
   protected static readonly TeventRouting<Note> NoteAppliers = TeventRouting<Note>.DeclareAppliers(it => it
     .For<INoteTevent.Created>(static (_, _) => {})
     .For<INoteTevent.Renamed>(static (note, tevent) => note.Name = tevent.Name)
     .For<INoteTevent.Renamed>(static (note, _) => note.NameAsTheLaterRenamedApplierFoundIt = note.Name)
     .IgnoreUnhandled<INoteTevent.Archived>());

   TeventRouting ITeventRouted.TeventAppliers => NoteAppliers;

   protected Note() {}

   public string Name { get; private set; } = "";

   ///<summary>The name as the second applier of a renaming found it: the new name, if the first, declared before it, ran first.</summary>
   public string NameAsTheLaterRenamedApplierFoundIt { get; private set; } = "";

   public void Rename(string name) => Publish(new NoteTevent.Renamed(name));

   public void Archive() => Publish(new NoteTevent.Archived());

   protected void PublishCreated() => Publish(new NoteTevent.Created(new TaggregateId()));
}

///<summary>A subclass with a component of its own, as JAStudio's VocabNote is.</summary>
class VocabNote : Note, ITeventRouted
{
   static readonly TeventRouting<VocabNote> VocabNoteAppliers = NoteAppliers.Extend<VocabNote>(it => it
     .For<INoteTevent.Renamed>(static (vocab, _) => vocab.QuestionAsTheVocabsRenamedApplierFoundIt = vocab.Question)
     .Component(static vocab => vocab.Forms)
     .Entities(static vocab => vocab._senses)
     .SharedTomponent(static vocab => vocab._audioSlot));

   static readonly TeventRouting<VocabNote> VocabNoteHandlers = TeventRouting<VocabNote>.DeclareHandlers(it => it
     .Component(static vocab => vocab.Forms));

   TeventRouting ITeventRouted.TeventAppliers => VocabNoteAppliers;
   TeventRouting ITeventRouted.TeventHandlers => VocabNoteHandlers;
   protected override Type WrapperTeventImplementation => typeof(VocabTevent<VocabTevent>);

   readonly VocabSense.ICollectionManager _senses;
   readonly SharedTomponentSlot<INoteTevent, NoteTevent, IAudioListTevent, IVocabAudioTevent<IAudioListTevent>> _audioSlot;

   VocabNote()
   {
      Forms = new FormsComponent(this);
      _senses = VocabSense.CreateSelfManagingCollection(this);
      _audioSlot = new SharedTomponentSlot<INoteTevent, NoteTevent, IAudioListTevent, IVocabAudioTevent<IAudioListTevent>>(this, typeof(VocabAudioTevent<IAudioListTevent>));
      Audio = new AudioList(_audioSlot);
   }

   public FormsComponent Forms { get; }
   public AudioList Audio { get; }
   public IReadOnlyEntityCollection<VocabSense, Guid> Senses => _senses.Entities;

   public VocabSense AddSense(Guid senseId, string gloss) => _senses.AddByPublishing(new VocabTevent.Sense.Added(senseId, gloss));

   ///<summary>A member only a vocab has, for the component to read through its typed parent.</summary>
   public string Question => $"{Name}?";

   ///<summary>The question as the vocab's own applier of a renaming found it: the new name's, if the note's appliers ran first.</summary>
   public string QuestionAsTheVocabsRenamedApplierFoundIt { get; private set; } = "";

   public static VocabNote Create()
   {
      var vocab = new VocabNote();
      vocab.PublishCreated();
      return vocab;
   }

   public static VocabNote LoadFromHistory(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history)
   {
      var vocab = new VocabNote();
      ((ITaggregate)vocab).LoadFromHistory(history);
      return vocab;
   }
}

///<summary>A component typed on the subclass that declares it, through the top-level <see cref="TeventiveComponent{TParent,TParentTevent,TParentTeventImplementation,TComponent,TComponentTevent,TComponentTeventImplementation}"/>,<br/>
/// with tevent appliers and tevent handlers of its own.</summary>
class FormsComponent : TeventiveComponent<VocabNote, INoteTevent, NoteTevent, FormsComponent, IVocabTevent.Forms, VocabTevent.Forms>, ITeventRouted
{
   static readonly TeventRouting<FormsComponent> FormsAppliers = TeventRouting<FormsComponent>.DeclareAppliers(it => it
     .For<IVocabTevent.Forms.Updated>(static (forms, tevent) =>
      {
         forms.All = tevent.AllForms.Split(',');
         forms.Summary = $"{forms._vocab.Question} {tevent.AllForms}";
      })
     .For<IVocabTevent.Forms.Counted>(static (forms, tevent) => forms.Count = tevent.Count));

   static readonly TeventRouting<FormsComponent> FormsHandlers = TeventRouting<FormsComponent>.DeclareHandlers(it => it
     .For<IVocabTevent.Forms.Updated>(static (forms, _) => forms.Publish(new VocabTevent.Forms.Counted(forms.All.Count))));

   TeventRouting ITeventRouted.TeventAppliers => FormsAppliers;
   TeventRouting ITeventRouted.TeventHandlers => FormsHandlers;

   readonly VocabNote _vocab;

   public FormsComponent(VocabNote vocab) : base(vocab) => _vocab = vocab;

   public IReadOnlyList<string> All { get; private set; } = [];
   public string Summary { get; private set; } = "";
   public int Count { get; private set; }

   public void Update(string allForms) => Publish(new VocabTevent.Forms.Updated(allForms));
   public void PublishATeventNoApplierHandles() => Publish(new VocabTevent.Forms.RoutedNowhere());
}

///<summary>An entity of a vocab, in a collection the vocab's routing routes to, with a routing of its own.</summary>
class VocabSense : Tentity<VocabNote, INoteTevent, NoteTevent, VocabSense, Guid, VocabTevent.Sense, IVocabTevent.Sense, IVocabTevent.Sense.Added, VocabTevent.Sense.IdGetterSetter>, ITeventRouted
{
   static readonly TeventRouting<VocabSense> SenseAppliers = TeventRouting<VocabSense>.DeclareAppliers(it => it
     .For<IVocabTevent.Sense.Defined>(static (sense, tevent) => sense.Gloss = tevent.Gloss));

   TeventRouting ITeventRouted.TeventAppliers => SenseAppliers;

   public VocabSense(VocabNote vocab) : base(vocab) {}

   public string Gloss { get; private set; } = "";

   public void Redefine(string gloss) => Publish(new VocabTevent.Sense.Redefined(gloss));
}

///<summary>A shared tomponent with a routing of its own.</summary>
class AudioList : SharedTomponent<IAudioListTevent>, ITeventRouted
{
   static readonly TeventRouting<AudioList> AudioListAppliers = TeventRouting<AudioList>.DeclareAppliers(it => it
     .For<IAudioListTevent.Added>(static (audio, tevent) => audio._files.Add(tevent.File)));

   TeventRouting ITeventRouted.TeventAppliers => AudioListAppliers;

   readonly List<string> _files = [];

   public AudioList(ISharedTomponentSlot<IAudioListTevent> slot) : base(slot) {}

   public IReadOnlyList<string> Files => _files;

   public void Add(string file) => Publish(new AudioListTevent.Added(file));
}
