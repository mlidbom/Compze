using System;
using System.Collections.Generic;
using System.Linq;
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
public class Note : Taggregate<Note, INoteTevent, NoteTevent, INoteTevent<INoteTevent>, NoteTevent<NoteTevent>>
{
   public string Name { get; private set; } = "";
   Note() => RegisterTeventAppliers().For<INoteTevent.Created>(_ => {}).For<INoteTevent.Renamed>(it => Name = it.Name);
   public static Note Create() { var note = new Note(); note.Publish(new NoteTevent.Created(new TaggregateId())); return note; }
   public void Rename(string name) => Publish(new NoteTevent.Renamed(name));
}

public static class Program
{
   static string Names(IEnumerable<ITaggregateTevent<ITaggregateTevent>> tevents) => string.Join(", ", tevents.Select(it => it.Tevent is INoteTevent.Renamed renamed ? $"Renamed({renamed.Name})" : it.Tevent.GetType().Name));

   public static void Main()
   {
      //A commit callback that dispatches the tevents, where a subscriber reacts by publishing on the same note.
      var note = Note.Create();
      note.Rename("first");
      try
      {
         ((ITaggregate)note).Commit(tevents =>
         {
            foreach(var tevent in tevents)
               if(tevent.Tevent is INoteTevent.Renamed { Name: "first" }) note.Rename("reaction");
         });
         Console.WriteLine("Publishing while the commit callback enumerates: no exception");
      }
      catch(Exception exception)
      {
         Console.WriteLine($"Publishing while the commit callback enumerates threw {exception.GetType().Name}: {exception.Message}");
      }

      //A commit callback that publishes without enumerating, then a second commit.
      var other = Note.Create();
      other.Rename("first");
      var firstCommit = "";
      ((ITaggregate)other).Commit(tevents =>
      {
         firstCommit = Names(tevents.ToList());
         other.Rename("published inside the commit callback");
      });
      var secondCommit = "";
      ((ITaggregate)other).Commit(tevents => secondCommit = Names(tevents));
      Console.WriteLine($"First commit handed over: {firstCommit}");
      Console.WriteLine($"Second commit handed over: [{secondCommit}]  while the note's name is '{other.Name}' at version {other.Version}");
   }
}
