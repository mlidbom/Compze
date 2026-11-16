using Compze.Abstractions;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Must;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.SnapshotsAndPublishCascades;

///<summary>An independent review's experiments against the combined prototype with static slots: what the design's one-thread assertion and its<br/>
/// per-cascade hook do in two cases the prototypes' own specifications don't cover.</summary>
public static class Review_findings_specification
{
   ///<summary>The tevent store's session hands each tevent to the bus with <c>PublishAsync(...).GetAwaiter().GetResult()</c>, and a participation handler,<br/>
   /// run in the same unit of work, gets the same taggregate from the same session. A handler that awaits real I/O resumes on a thread-pool thread while the<br/>
   /// publishing thread waits for it. <see cref="Task.Run(System.Action)"/> with a wait stands in for that hop.</summary>
   public class Given_a_note_observed_by_something_that_publishes_on_it_from_another_thread_while_the_observing_thread_waits_for_it
   {
      readonly Note _note = Note.Create("created");

      public Given_a_note_observed_by_something_that_publishes_on_it_from_another_thread_while_the_observing_thread_waits_for_it() =>
         ((ITaggregate)_note).TeventStream.Subscribe(it =>
         {
            if(it.Tevent is INoteTevent.Edited) Task.Run(() => _note.React()).GetAwaiter().GetResult();
         });

      [XF] public void editing_it_throws_that_a_taggregate_is_used_by_one_thread_at_a_time() =>
         Invoking(() => _note.Edit("edited")).Must().Throw<InvalidOperationException>().Which.Message.Must().Contain("one thread at a time");
   }

   public class Given_a_note_class_whose_override_of_OnPublishCascadeEnded_does_not_call_the_base_implementation
   {
      readonly NoteWhoseHookDoesNotCallTheBase _note = NoteWhoseHookDoesNotCallTheBase.Create("created");
      readonly List<Type> _teventsObserved = [];

      public Given_a_note_class_whose_override_of_OnPublishCascadeEnded_does_not_call_the_base_implementation() =>
         ((ITaggregate)_note).TeventStream.Subscribe(it => _teventsObserved.Add(it.Tevent.GetType()));

      public class when_it_is_edited : Given_a_note_class_whose_override_of_OnPublishCascadeEnded_does_not_call_the_base_implementation
      {
         public when_it_is_edited() => _note.Edit("edited");

         [XF] public void the_tevent_stream_emits_nothing() => _teventsObserved.Must().HaveCount(0);
      }
   }
}

///<summary>A <see cref="Note"/> whose class overrides the per-cascade hook, as JAStudio's JPNote would, and doesn't call the base implementation.</summary>
class NoteWhoseHookDoesNotCallTheBase : Note
{
   public static new NoteWhoseHookDoesNotCallTheBase Create(string text)
   {
      var note = new NoteWhoseHookDoesNotCallTheBase();
      note.Publish(new NoteTevent.Created(new TaggregateId(), text));
      return note;
   }

   protected override void OnPublishCascadeEnded(IReadOnlyList<ITaggregateTevent<INoteTevent>> teventsOfTheCascade) {}
}
