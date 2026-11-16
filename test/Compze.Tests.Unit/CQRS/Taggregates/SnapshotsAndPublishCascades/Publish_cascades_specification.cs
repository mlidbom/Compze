using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Must;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.SnapshotsAndPublishCascades;

///<summary>A publish cascade is one call to Publish together with every Publish that tevent handlers make from inside it. When it ends the invariants are<br/>
/// asserted and the taggregate's OnPublishCascadeEnded is called once, with its tevents; whatever that hands them to may publish on the same taggregate,<br/>
/// which starts a cascade of its own.</summary>
public static class Publish_cascades_specification
{
   static readonly Type[] EditedThenReacted = [typeof(NoteTevent.Edited), typeof(NoteTevent.Reacted)];

   public class Given_a_note_whose_tevent_handler_reacts_to_an_edit_by_publishing
   {
      readonly Note _note = Note.Create("created");
      readonly List<Type> _teventsObserved = [];

      public Given_a_note_whose_tevent_handler_reacts_to_an_edit_by_publishing()
      {
         _note.ReactToEditsInATeventHandler();
         ((ITaggregate)_note).TeventStream.Subscribe(it => _teventsObserved.Add(it.Tevent.GetType()));
      }

      public class when_it_is_edited : Given_a_note_whose_tevent_handler_reacts_to_an_edit_by_publishing
      {
         public when_it_is_edited() => _note.Edit("edited");

         [XF] public void one_publish_cascade_ended_with_the_edit_and_the_reaction_in_order() =>
            _note.PublishCascadesEnded.Skip(1).Single().Select(it => it.GetType()).Must().SequenceEqual(EditedThenReacted);

         [XF] public void the_tevent_stream_emitted_the_edit_and_the_reaction_in_order() => _teventsObserved.Must().SequenceEqual(EditedThenReacted);
      }
   }

   public class Given_a_note_observed_by_something_that_publishes_on_it_in_reaction_to_an_edit
   {
      readonly Note _note = Note.Create("created");
      readonly List<Type> _teventsObserved = [];

      public Given_a_note_observed_by_something_that_publishes_on_it_in_reaction_to_an_edit() =>
         ((ITaggregate)_note).TeventStream.Subscribe(it =>
         {
            _teventsObserved.Add(it.Tevent.GetType());
            if(it.Tevent is INoteTevent.Edited) _note.React();
         });

      public class when_it_is_edited : Given_a_note_observed_by_something_that_publishes_on_it_in_reaction_to_an_edit
      {
         public when_it_is_edited() => _note.Edit("edited");

         [XF] public void the_observer_saw_each_tevent_once_in_order() => _teventsObserved.Must().SequenceEqual(EditedThenReacted);

         [XF] public void the_edit_and_the_reaction_ended_a_publish_cascade_each() =>
            _note.PublishCascadesEnded.Skip(1).Select(it => it.Single().GetType()).Must().SequenceEqual(EditedThenReacted);

         [XF] public void both_are_uncommitted_in_order() =>
            ((ITaggregate)_note).Commit(tevents => tevents.Skip(1).Select(it => it.Tevent.GetType()).Must().SequenceEqual(EditedThenReacted));
      }
   }

   ///<summary>The order the jastudio-rep-fable session showed a nested notification breaks: an edit and its tevent handler's reaction are one cascade,<br/>
   /// and an observer marks the note when it is told of the edit. Published as edit, reaction, mark, it must be delivered in that order: the tevent<br/>
   /// store's session saves each tevent as it is told of it, so it would otherwise save version 3 before version 2.</summary>
   public class Given_a_note_whose_handler_reacts_to_an_edit_and_whose_observer_marks_it_when_told_of_the_edit
   {
      readonly Note _note = Note.Create("created");
      readonly List<Type> _teventsObserved = [];

      public Given_a_note_whose_handler_reacts_to_an_edit_and_whose_observer_marks_it_when_told_of_the_edit()
      {
         _note.ReactToEditsInATeventHandler();
         ((ITaggregate)_note).TeventStream.Subscribe(it =>
         {
            _teventsObserved.Add(it.Tevent.GetType());
            if(it.Tevent is INoteTevent.Edited) _note.Mark();
         });
         _note.Edit("edited");
      }

      [XF] public void the_observer_is_told_of_the_tevents_in_the_order_they_were_published() =>
         _teventsObserved.Must().SequenceEqual([typeof(NoteTevent.Edited), typeof(NoteTevent.Reacted), typeof(NoteTevent.Marked)]);

      [XF] public void the_tevents_are_uncommitted_in_the_order_they_were_published() =>
         ((ITaggregate)_note).Commit(tevents => tevents.Skip(1).Select(it => it.Tevent.GetType()).Must().SequenceEqual([typeof(NoteTevent.Edited), typeof(NoteTevent.Reacted), typeof(NoteTevent.Marked)]));
   }

   public class Given_a_note_whose_hook_throws_after_an_observer_marked_it_when_told_of_an_edit
   {
      readonly Note _note = Note.Create("created");

      public Given_a_note_whose_hook_throws_after_an_observer_marked_it_when_told_of_an_edit()
      {
         ((ITaggregate)_note).TeventStream.Subscribe(it =>
         {
            if(it.Tevent is INoteTevent.Edited) _note.Mark();
         });
         var hasThrown = false;
         _note.WhenAPublishCascadeEnds(_ =>
         {
            if(hasThrown) return;
            hasThrown = true;
            throw new InvalidOperationException("The hook was told to throw once.");
         });
      }

      [XF] public void the_edit_throws_out_of_the_hook_with_the_edit_applied()
      {
         Invoking(() => _note.Edit("edited")).Must().Throw<InvalidOperationException>();
         _note.Text.Must().Be("edited");
      }

      [XF] public void the_mark_queued_before_the_hook_threw_is_handed_on_before_the_next_publish()
      {
         Invoking(() => _note.Edit("edited")).Must().Throw<InvalidOperationException>();
         _note.React();
         _note.PublishCascadesEnded.Skip(1).Select(it => it.Single().GetType()).Must().SequenceEqual([typeof(NoteTevent.Edited), typeof(NoteTevent.Marked), typeof(NoteTevent.Reacted)]);
      }
   }

   public class Given_a_note_whose_tevent_applier_threw_from_inside_a_publish_cascade
   {
      readonly Note _note = Note.Create("created");

      public Given_a_note_whose_tevent_applier_threw_from_inside_a_publish_cascade() =>
         Invoking(() => _note.Edit(Note.TextTheApplierThrowsOn)).Must().Throw<InvalidOperationException>();

      [XF] public void publishing_on_it_again_is_refused() => Invoking(() => _note.React()).Must().Throw<Exception>();
      [XF] public void committing_it_is_refused() => Invoking(() => ((ITaggregate)_note).Commit(_ => {})).Must().Throw<Exception>();
   }

   public class Given_a_note_that_commits_at_the_end_of_each_publish_cascade_and_then_reacts_to_an_edit_by_publishing
   {
      readonly Note _note = Note.Create("created");
      readonly List<IReadOnlyList<Type>> _commits = [];

      public Given_a_note_that_commits_at_the_end_of_each_publish_cascade_and_then_reacts_to_an_edit_by_publishing()
      {
         ((ITaggregate)_note).Commit(_ => {});
         _note.WhenAPublishCascadeEnds(note =>
         {
            var committedAnEdit = false;
            ((ITaggregate)note).Commit(tevents =>
            {
               _commits.Add([..tevents.Select(it => it.Tevent.GetType())]);
               committedAnEdit = tevents.Any(it => it.Tevent is INoteTevent.Edited);
            });
            if(committedAnEdit) note.React();
         });
      }

      public class when_it_is_edited : Given_a_note_that_commits_at_the_end_of_each_publish_cascade_and_then_reacts_to_an_edit_by_publishing
      {
         public when_it_is_edited() => _note.Edit("edited");

         [XF] public void the_edit_and_the_reaction_were_committed_once_each_in_order() =>
            _commits.Select(it => it.Single()).Must().SequenceEqual(EditedThenReacted);

         [XF] public void nothing_is_left_uncommitted() => ((ITaggregate)_note).Commit(tevents => tevents.Must().BeEmpty());
      }
   }

   public class Given_a_note_whose_commit_callback_publishes_on_it_while_enumerating_the_tevents_it_commits
   {
      readonly Note _note = Note.Create("created");
      readonly List<Type> _committedFirst = [];

      public Given_a_note_whose_commit_callback_publishes_on_it_while_enumerating_the_tevents_it_commits()
      {
         _note.Edit("edited");
         ((ITaggregate)_note).Commit(tevents =>
         {
            foreach(var it in tevents)
            {
               _committedFirst.Add(it.Tevent.GetType());
               if(it.Tevent is INoteTevent.Edited) _note.React();
            }
         });
      }

      [XF] public void the_callback_was_handed_the_tevents_published_before_the_commit() =>
         _committedFirst.Must().SequenceEqual([typeof(NoteTevent.Created), typeof(NoteTevent.Edited)]);

      [XF] public void the_tevent_it_published_is_left_for_the_next_commit() =>
         ((ITaggregate)_note).Commit(tevents => tevents.Select(it => it.Tevent.GetType()).Must().SequenceEqual([typeof(NoteTevent.Reacted)]));
   }

   public class Given_a_note_whose_edit_on_another_thread_waits_inside_its_publish_cascade : IDisposable
   {
      readonly Note _note = Note.Create("created");
      readonly IThreadGate _gateInsideTheCascade = IThreadGate.NewClosed(WaitTimeout.Seconds(10));
      readonly TestingTaskRunner _taskRunner = TestingTaskRunner.WithTimeout(TimeSpan.FromSeconds(30));

      public Given_a_note_whose_edit_on_another_thread_waits_inside_its_publish_cascade()
      {
         _note.MakeEditsWaitAt(_gateInsideTheCascade);
         _taskRunner.Run(() => _note.Edit("edited on the other thread"));
         _gateInsideTheCascade.AwaitQueueLengthEqualTo(1);
      }

      [XF] public void publishing_on_it_from_this_thread_fails() => Invoking(() => _note.React()).Must().Throw<InvalidOperationException>();

      [XF] public void committing_it_from_this_thread_fails() => Invoking(() => ((ITaggregate)_note).Commit(_ => {})).Must().Throw<InvalidOperationException>();

      [XF] public void once_the_other_thread_is_done_this_thread_can_publish_on_it()
      {
         _gateInsideTheCascade.Open();
         _taskRunner.Dispose();
         _note.React();
         _note.Text.Must().Be("edited on the other thread");
      }

      public void Dispose()
      {
         _gateInsideTheCascade.Open();
         _taskRunner.Dispose();
      }
   }
}
