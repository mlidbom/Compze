using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;

// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CA1812 // Avoid uninstantiated internal classes # used via reflection
#pragma warning disable CA1715 // Nested tevent interfaces follow the semantic events naming convention

namespace Compze.Tests.Unit.CQRS.Taggregates.PerClassTeventRouting;

interface INoteTevent : ITaggregateTevent
{
   interface Created : INoteTevent, ITaggregateCreatedTevent;
   interface Renamed : INoteTevent { string Name { get; } }
}

interface INoteTevent<out T> : ITaggregateTevent<T> where T : INoteTevent;
class NoteTevent<T>(T tevent) : TaggregateTevent<T>(tevent), INoteTevent<T> where T : INoteTevent;

class NoteTevent : TaggregateTevent, INoteTevent
{
   protected NoteTevent() {}
   NoteTevent(TaggregateId taggregateId) : base(taggregateId) {}

   internal class Created(TaggregateId taggregateId) : NoteTevent(taggregateId), INoteTevent.Created;
   internal class Renamed(string name) : NoteTevent, INoteTevent.Renamed { public string Name { get; } = name; }
}

interface IVocabTevent : INoteTevent
{
   interface Forms : IVocabTevent
   {
      interface Updated : Forms { string AllForms { get; } }
      interface Counted : Forms { int Count { get; } }
      interface RoutedNowhere : Forms;
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
}

///<summary>The root of an inheritance hierarchy of taggregates, as JAStudio's JPNote is: it closes the <see cref="Taggregate{TTaggregate,TTaggregateTevent,TTaggregateTeventImplementation,TWrapperTeventInterface,TWrapperTeventImplementation}"/> generic.</summary>
class Note : Taggregate<Note, INoteTevent, NoteTevent, INoteTevent<INoteTevent>, NoteTevent<NoteTevent>>
{
   protected static readonly TeventRouting<Note> NoteAppliers = TeventRouting<Note>.DeclareAppliers(it => it
     .For<INoteTevent.Created>(static (_, _) => {})
     .For<INoteTevent.Renamed>(static (note, tevent) => note.Name = tevent.Name));

   protected override TeventRouting TeventAppliers => NoteAppliers;

   protected Note() {}

   public string Name { get; private set; } = "";

   public void Rename(string name) => Publish(new NoteTevent.Renamed(name));

   protected void PublishCreated() => Publish(new NoteTevent.Created(new TaggregateId()));
}

///<summary>A subclass with a component of its own, as JAStudio's VocabNote is.</summary>
class VocabNote : Note
{
   static readonly TeventRouting<VocabNote> VocabNoteAppliers = NoteAppliers.Extend<VocabNote>(it => it
     .Component(static vocab => vocab.Forms));

   static readonly TeventRouting<VocabNote> VocabNoteHandlers = TeventRouting<VocabNote>.DeclareHandlers(it => it
     .Component(static vocab => vocab.Forms));

   protected override TeventRouting TeventAppliers => VocabNoteAppliers;
   protected override TeventRouting TeventHandlers => VocabNoteHandlers;
   protected override Type WrapperTeventImplementation => typeof(VocabTevent<VocabTevent>);

   VocabNote() => Forms = new FormsComponent(this);

   public FormsComponent Forms { get; }

   ///<summary>A member only a vocab has, for the component to read through its typed parent.</summary>
   public string Question => $"{Name}?";

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
class FormsComponent : TeventiveComponent<VocabNote, INoteTevent, NoteTevent, FormsComponent, IVocabTevent.Forms, VocabTevent.Forms>
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

   protected override TeventRouting TeventAppliers => FormsAppliers;
   protected override TeventRouting TeventHandlers => FormsHandlers;

   readonly VocabNote _vocab;

   public FormsComponent(VocabNote vocab) : base(vocab) => _vocab = vocab;

   public IReadOnlyList<string> All { get; private set; } = [];
   public string Summary { get; private set; } = "";
   public int Count { get; private set; }

   public void Update(string allForms) => Publish(new VocabTevent.Forms.Updated(allForms));
   public void PublishATeventNoApplierHandles() => Publish(new VocabTevent.Forms.RoutedNowhere());
}
