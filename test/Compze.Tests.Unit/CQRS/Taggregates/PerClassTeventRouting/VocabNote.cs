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

   ///<summary>The tevents of a vocab's examples: removable entities. Adding one carries nothing but its id.</summary>
   interface Example : IVocabTevent
   {
      Guid ExampleId { get; }
      interface Added : Example;
      interface Edited : Example { string Text { get; } }
      interface Removed : Example;
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

   internal abstract class Example : VocabTevent, IVocabTevent.Example
   {
      public Guid ExampleId { get; private set; }

      internal class IdGetterSetter : IGetSetTaggregateEntityTeventEntityId<Guid, Example, IVocabTevent.Example>
      {
         public void SetEntityId(Example tevent, Guid id) => tevent.ExampleId = id;
         public Guid GetId(IVocabTevent.Example tevent) => tevent.ExampleId;
      }

      internal class Added : Example, IVocabTevent.Example.Added
      {
         public Added(Guid exampleId) => ExampleId = exampleId;
      }

      internal class Edited(string text) : Example, IVocabTevent.Example.Edited { public string Text { get; } = text; }
      internal class Removed : Example, IVocabTevent.Example.Removed;
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
class Note : Taggregate<Note, INoteTevent, NoteTevent, INoteTevent<INoteTevent>, NoteTevent<NoteTevent>>
{
   protected static readonly TeventRouting<Note, INoteTevent> NoteAppliers = DeclareAppliers(it => it
     .For<INoteTevent.Created>(static (_, _) => {})
     .For<INoteTevent.Renamed>(static (note, tevent) => note.Name = tevent.Name)
     .For<INoteTevent.Renamed>(static (note, _) => note.NameAsTheLaterRenamedApplierFoundIt = note.Name)
     .IgnoreUnhandled<INoteTevent.Archived>());

   ///<summary>None: the note declares its tevent handlers for its subclasses to extend, since a taggregate's tevent handlers start at the root, as its appliers do.</summary>
   protected static readonly TeventRouting<Note, INoteTevent> NoteHandlers = DeclareHandlers(_ => {});

   protected override TeventRouting TeventAppliers => NoteAppliers;
   protected override TeventRouting TeventHandlers => NoteHandlers;

   protected Note() {}

   public string Name { get; private set; } = "";

   ///<summary>The name as the second applier of a renaming found it: the new name, if the first, declared before it, ran first.</summary>
   public string NameAsTheLaterRenamedApplierFoundIt { get; private set; } = "";

   public void Rename(string name) => Publish(new NoteTevent.Renamed(name));

   public void Archive() => Publish(new NoteTevent.Archived());

   protected void PublishCreated() => Publish(new NoteTevent.Created(new TaggregateId()));
}

///<summary>A subclass with a component of its own, as JAStudio's VocabNote is.</summary>
class VocabNote : Note
{
   static class Slots
   {
      internal static readonly SharedTomponentSlotOfAClass<VocabNote, INoteTevent, NoteTevent, IAudioListTevent, IVocabAudioTevent<IAudioListTevent>> Audio =
         new(typeof(VocabAudioTevent<>), static vocab => vocab.Audio);
   }

   static readonly TeventRouting<VocabNote, INoteTevent> VocabNoteAppliers = NoteAppliers.Extend<VocabNote>(it => it
     .For<INoteTevent.Renamed>(static (vocab, _) => vocab.QuestionAsTheVocabsRenamedApplierFoundIt = vocab.Question)
     .Component(static vocab => vocab.Forms)
     .Entities(static vocab => vocab._senses)
     .Entities(static vocab => vocab._examples)
     .SharedTomponent(Slots.Audio));

   static readonly TeventRouting<VocabNote, INoteTevent> VocabNoteHandlers = NoteHandlers.Extend<VocabNote>(it => it
     .For<IVocabTevent.Forms.Updated>(static (vocab, _) => vocab.Forms.PublishTheCount()));

   protected override TeventRouting TeventAppliers => VocabNoteAppliers;
   protected override TeventRouting TeventHandlers => VocabNoteHandlers;
   protected override Type WrapperTeventImplementation => typeof(VocabTevent<VocabTevent>);

   readonly VocabSense.ICollectionManager _senses;
   readonly VocabExample.ICollectionManager _examples;

   VocabNote()
   {
      Forms = new FormsComponent(this);
      _senses = VocabSense.CreateSelfManagingCollection(this);
      _examples = VocabExample.CreateSelfManagingCollection(this);
      Audio = new AudioList(Slots.Audio.In(this));
   }

   public FormsComponent Forms { get; }
   public AudioList Audio { get; }
   public IReadOnlyEntityCollection<VocabSense, Guid> Senses => _senses.Entities;
   public IReadOnlyEntityCollection<VocabExample, Guid> Examples => _examples.Entities;

   public VocabSense AddSense(Guid senseId, string gloss) => _senses.AddByPublishing(new VocabTevent.Sense.Added(senseId, gloss));
   public VocabExample AddExample(Guid exampleId) => _examples.AddByPublishing(new VocabTevent.Example.Added(exampleId));

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
/// with tevent appliers of its own. A tevent handler of the vocab publishes its count.</summary>
class FormsComponent : TeventiveComponent<VocabNote, INoteTevent, NoteTevent, FormsComponent, IVocabTevent.Forms, VocabTevent.Forms>
{
   static readonly TeventRouting<FormsComponent, IVocabTevent.Forms> FormsAppliers = DeclareAppliers(it => it
     .For<IVocabTevent.Forms.Updated>(static (forms, tevent) =>
      {
         forms.All = tevent.AllForms.Split(',');
         forms.Summary = $"{forms._vocab.Question} {tevent.AllForms}";
      })
     .For<IVocabTevent.Forms.Counted>(static (forms, tevent) => forms.Count = tevent.Count));

   protected override TeventRouting TeventAppliers => FormsAppliers;

   readonly VocabNote _vocab;

   public FormsComponent(VocabNote vocab) : base(vocab) => _vocab = vocab;

   public IReadOnlyList<string> All { get; private set; } = [];
   public string Summary { get; private set; } = "";
   public int Count { get; private set; }

   public void Update(string allForms) => Publish(new VocabTevent.Forms.Updated(allForms));
   public void PublishTheCount() => Publish(new VocabTevent.Forms.Counted(All.Count));
   public void PublishATeventNoApplierHandles() => Publish(new VocabTevent.Forms.RoutedNowhere());
}

///<summary>An entity of a vocab, in a collection the vocab's routing routes to, with a routing of its own.</summary>
class VocabSense : Tentity<VocabNote, INoteTevent, NoteTevent, VocabSense, Guid, VocabTevent.Sense, IVocabTevent.Sense, IVocabTevent.Sense.Added, VocabTevent.Sense.IdGetterSetter>
{
   static readonly TeventRouting<VocabSense, IVocabTevent.Sense> SenseAppliers = DeclareAppliers(it => it
     .For<IVocabTevent.Sense.Defined>(static (sense, tevent) => sense.Gloss = tevent.Gloss));

   protected override TeventRouting TeventAppliers => SenseAppliers;

   public VocabSense(VocabNote vocab) : base(vocab) {}

   public string Gloss { get; private set; } = "";

   public void Redefine(string gloss) => Publish(new VocabTevent.Sense.Redefined(gloss));
}

///<summary>A removable entity of a vocab. Its routing has no applier of its created tevent, which gives it its id all the same, and one of its<br/>
/// removal, which records whether it was still in the vocab's collection then.</summary>
class VocabExample : TeventiveRemovableEntity<VocabNote, INoteTevent, NoteTevent, VocabExample, Guid, IVocabTevent.Example, VocabTevent.Example, IVocabTevent.Example.Added, IVocabTevent.Example.Removed, VocabTevent.Example.IdGetterSetter>
{
   static readonly TeventRouting<VocabExample, IVocabTevent.Example> ExampleAppliers = DeclareAppliers(it => it
     .For<IVocabTevent.Example.Edited>(static (example, tevent) => example.Text = tevent.Text)
     .For<IVocabTevent.Example.Removed>(static (example, _) => example.WasInTheVocabsExamplesWhenItAppliedItsRemoval = example._vocab.Examples.Contains(example.Id)));

   protected override TeventRouting TeventAppliers => ExampleAppliers;

   readonly VocabNote _vocab;

   public VocabExample(VocabNote vocab) : base(vocab) => _vocab = vocab;

   public string Text { get; private set; } = "";
   public bool WasInTheVocabsExamplesWhenItAppliedItsRemoval { get; private set; }

   public void Edit(string text) => Publish(new VocabTevent.Example.Edited(text));
   public void Remove() => Publish(new VocabTevent.Example.Removed());
}

///<summary>A shared tomponent with a routing of its own.</summary>
class AudioList : SharedTomponent<IAudioListTevent>
{
   static readonly TeventRouting<AudioList, IAudioListTevent> AudioListAppliers = DeclareAppliers<AudioList>(it => it
     .For<IAudioListTevent.Added>(static (audio, tevent) => audio._files.Add(tevent.File)));

   protected override TeventRouting TeventAppliers => AudioListAppliers;

   readonly List<string> _files = [];

   public AudioList(SharedTomponentSlotInAnOwner<IAudioListTevent> slot) : base(slot) {}

   public IReadOnlyList<string> Files => _files;

   public void Add(string file) => Publish(new AudioListTevent.Added(file));
}
