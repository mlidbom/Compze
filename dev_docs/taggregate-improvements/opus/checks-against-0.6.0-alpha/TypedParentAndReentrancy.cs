using System;
using System.Collections.Generic;
using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;
#pragma warning disable
namespace Checks;

public interface INoteTevent : ITaggregateTevent
{
   interface Created : INoteTevent, ITaggregateCreatedTevent;
   interface Renamed : INoteTevent { string Name { get; } }
}
public interface INoteTevent<out T> : ITaggregateTevent<T> where T : INoteTevent;
public class NoteTevent<T>(T tevent) : TaggregateTevent<T>(tevent), INoteTevent<T> where T : INoteTevent;
public abstract class NoteTevent : TaggregateTevent, INoteTevent
{
   protected NoteTevent() {}
   protected NoteTevent(TaggregateId id) : base(id) {}
   public class Created(TaggregateId id) : NoteTevent(id), INoteTevent.Created;
   public class Renamed(string name) : NoteTevent, INoteTevent.Renamed { public string Name { get; } = name; }
}

//The root of the inheritance hierarchy closes the Taggregate generic, as JPNote does.
public class JPNote : Taggregate<JPNote, INoteTevent, NoteTevent, INoteTevent<INoteTevent>, NoteTevent<NoteTevent>>
{
   public string Name { get; private set; } = "";
   protected JPNote()
   {
      RegisterTeventAppliers()
        .For<INoteTevent.Created>(_ => {})
        .For<INoteTevent.Renamed>(it => Name = it.Name);
   }
   public void Rename(string name) => Publish(new NoteTevent.Renamed(name));
   protected void Create() => Publish(new NoteTevent.Created(new TaggregateId()));
}

public interface IVocabTevent : INoteTevent
{
   interface Forms : IVocabTevent
   {
      interface Updated : Forms { string Value { get; } }
   }
}
public abstract class VocabTevent : NoteTevent, IVocabTevent
{
   public abstract class Forms : VocabTevent, IVocabTevent.Forms
   {
      public class Updated(string value) : Forms, IVocabTevent.Forms.Updated { public string Value { get; } = value; }
   }
}

public class VocabNote : JPNote
{
   public FormsComponent Forms { get; }
   VocabNote() => Forms = new FormsComponent(this);
   public static VocabNote CreateNew() { var note = new VocabNote(); note.Create(); return note; }
   public string Question => "question of " + Name;
}

//A component typed on the subclass that declares it, through the top-level base class instead of the nested JPNote.Component shorthand.
public class FormsComponent : TeventiveComponent<VocabNote, INoteTevent, NoteTevent, FormsComponent, IVocabTevent.Forms, VocabTevent.Forms>
{
   readonly VocabNote _note;
   public string Value { get; private set; } = "";
   public FormsComponent(VocabNote note) : base(note)
   {
      _note = note;
      RegisterTeventAppliers().For<IVocabTevent.Forms.Updated>(it => Value = it.Value + " / " + _note.Question);
   }
   public void Update(string value) => Publish(new VocabTevent.Forms.Updated(value));
}

public static class Program
{
   public static void Main()
   {
      var vocab = VocabNote.CreateNew();
      vocab.Rename("taberu");
      vocab.Forms.Update("食べる");
      Console.WriteLine($"Typed-parent component: Forms.Value = '{vocab.Forms.Value}'");

      var note = VocabNote.CreateNew();
      ((ITaggregate)note).Commit(_ => {});
      var observed = new List<string>();
      var reacted = false;
      ((ITaggregate)note).TeventStream.Subscribe(new Observer(wrapped =>
      {
         observed.Add(wrapped.Tevent.GetType().Name);
         if(!reacted && wrapped.Tevent is INoteTevent.Renamed)
         {
            reacted = true;
            note.Forms.Update("reaction"); //An observer reacting to a change by publishing on the same taggregate.
         }
      }));
      try
      {
         note.Rename("first");
         Console.WriteLine("Reentrant publish from an observer: no exception");
      }
      catch(Exception exception)
      {
         Console.WriteLine($"Reentrant publish from an observer threw {exception.GetType().Name}: {exception.Message}");
      }
      Console.WriteLine($"Tevents the observer saw, in order: {string.Join(", ", observed)}");
   }

   class Observer(Action<ITaggregateTevent<ITaggregateTevent>> onNext) : IObserver<ITaggregateTevent<ITaggregateTevent>>
   {
      public void OnNext(ITaggregateTevent<ITaggregateTevent> value) => onNext(value);
      public void OnError(Exception error) {}
      public void OnCompleted() {}
   }
}
