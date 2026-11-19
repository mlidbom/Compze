using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Must;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Tests.Unit.CQRS.Taggregates.PerClassTeventRouting;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.HandingOutAndCommitting;

///<summary>A taggregate hands its tevents to the observers of its tevent stream once it is at rest, and its uncommitted tevents to<br/>
/// <see cref="ITaggregate.Commit"/>'s callback. What those publish on it from inside goes out after what was handed before: never from<br/>
/// inside them, and never lost.</summary>
public class Given_a_vocab_note
{
   readonly VocabNote _vocab = VocabNote.Create();
   readonly List<string> _log = [];

   public class whose_tevent_stream_an_observer_follows_that_renames_it_when_it_is_first_renamed : Given_a_vocab_note
   {
      public whose_tevent_stream_an_observer_follows_that_renames_it_when_it_is_first_renamed()
      {
         ((ITaggregate)_vocab).TeventStream.Subscribe(wrapped =>
         {
            if(wrapped.Tevent is not INoteTevent.Renamed renamed) return;
            _log.Add($"received {renamed.Name}");
            if(renamed.Name != "first") return;
            _vocab.Rename("second");
            _log.Add("the observer's rename returned");
         });
         _vocab.Rename("first");
      }

      [XF] public void the_observer_receives_each_tevent_once_the_one_it_published_after_its_own_call_returned() =>
         _log.Must().SequenceEqual(["received first", "the observer's rename returned", "received second"]);
   }

   public class whose_commit_callback_renames_it : Given_a_vocab_note
   {
      readonly List<ITaggregateTevent<ITaggregateTevent>> _handedToTheFirstCommit = [];
      readonly List<ITaggregateTevent<ITaggregateTevent>> _handedToTheSecondCommit = [];

      public whose_commit_callback_renames_it()
      {
         ((ITaggregate)_vocab).Commit(tevents =>
         {
            _handedToTheFirstCommit.AddRange(tevents);
            _vocab.Rename("during the commit");
         });
         ((ITaggregate)_vocab).Commit(_handedToTheSecondCommit.AddRange);
      }

      [XF] public void the_first_commit_hands_over_only_the_tevents_published_before_it() =>
         _handedToTheFirstCommit.Select(it => it.Tevent).Must().Satisfy(it => it.Single() is INoteTevent.Created);

      [XF] public void the_rename_made_from_its_callback_stays_uncommitted_for_the_next_commit() =>
         _handedToTheSecondCommit.Select(it => ((INoteTevent.Renamed)it.Tevent).Name).Must().SequenceEqual(["during the commit"]);
   }
}
