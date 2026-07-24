using Compze.Internals.Testing.Utilities;
using Compze.Internals.Testing.Utilities.Awaiting;
using Compze.Must;
using Compze.Threading;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for specification naming
#pragma warning disable IDE1006 //Reviewed OK: specification naming styles

namespace Compze.Internals.Testing.Specifications.Utilities;

///<summary>Specifies <see cref="PollAwaitExtensions.PollAwait{TThis}"/>: the wait that asks an object repeatedly whether a
/// condition holds, so that a specification never continues on a timer without having observed what it waited for.</summary>
public class When_poll_awaiting_a_condition
{
   readonly StateArrivingAfterSomeEvaluations _state = new(evaluationsBeforeItArrives: 3);

   public class that_arrives_while_waiting : When_poll_awaiting_a_condition
   {
      public that_arrives_while_waiting() => _state.PollAwait(it => it.HasArrived());

      [XF] public void the_wait_ends_once_the_state_has_arrived() => _state.Arrived.Must().BeTrue();
      [XF] public void the_condition_is_evaluated_until_it_holds_and_no_further() => _state.Evaluations.Must().Be(4);
   }

   public class that_never_arrives : When_poll_awaiting_a_condition
   {
      readonly PollAwaitTimeoutException _thrownException;

      public that_never_arrives() =>
         _thrownException = Invoking(() => _state.PollAwait(it => it.HasNeverArrived(), timeout: WaitTimeout.Milliseconds(20)))
                           .Must().Throw<PollAwaitTimeoutException>().Which;

      [XF] public void the_wait_throws_rather_than_letting_the_specification_continue_unsatisfied() => _thrownException.Must().NotBeNull();
      [XF] public void the_message_quotes_the_conditions_own_source_text() => _thrownException.Message.Must().Contain("it.HasNeverArrived()");
      [XF] public void the_message_names_how_long_it_waited() => _thrownException.Message.Must().Contain(WaitTimeout.Milliseconds(20).ToString());
      [XF] public void the_message_names_the_polled_object() => _thrownException.Message.Must().Contain(nameof(StateArrivingAfterSomeEvaluations));
   }

   ///<summary>The guarantee that separates polling from sleeping: reaching the deadline is never reported before the condition has
   /// been asked one last time, so a false answer always means observed-false rather than never-looked.</summary>
   public class whose_timeout_has_already_expired : When_poll_awaiting_a_condition
   {
      [XF] public void an_already_true_condition_is_still_observed_and_the_wait_succeeds() =>
         _state.TryPollAwait(it => it.HasAlreadyArrived(), timeout: WaitTimeout.Milliseconds(0)).Must().BeTrue();

      [XF] public void a_false_condition_is_evaluated_exactly_once_before_giving_up()
      {
         _state.TryPollAwait(it => it.HasNeverArrived(), timeout: WaitTimeout.Milliseconds(0)).Must().BeFalse();
         _state.Evaluations.Must().Be(1);
      }
   }

   public class through_TryPollAwait : When_poll_awaiting_a_condition
   {
      [XF] public void a_condition_that_arrives_reports_true() => _state.TryPollAwait(it => it.HasArrived()).Must().BeTrue();

      [XF] public void a_condition_that_never_arrives_reports_false_instead_of_throwing() =>
         _state.TryPollAwait(it => it.HasNeverArrived(), timeout: WaitTimeout.Milliseconds(20)).Must().BeFalse();
   }

   ///<summary>State that reports itself absent for a set number of evaluations and present from then on — a condition whose
   /// arrival a specification must wait for, with the waiting made countable.</summary>
   protected class StateArrivingAfterSomeEvaluations
   {
      readonly int _evaluationsBeforeItArrives;
      internal StateArrivingAfterSomeEvaluations(int evaluationsBeforeItArrives) => _evaluationsBeforeItArrives = evaluationsBeforeItArrives;

      ///<summary>How many times a condition has been evaluated against this state.</summary>
      internal int Evaluations { get; private set; }

      ///<summary>Whether the state has arrived, without counting as an evaluation — for asserting after the wait.</summary>
      internal bool Arrived => Evaluations > _evaluationsBeforeItArrives;

      internal bool HasArrived() => ++Evaluations > _evaluationsBeforeItArrives;
      internal bool HasAlreadyArrived() => ++Evaluations > 0;
      internal bool HasNeverArrived() => ++Evaluations < 0;
   }
}
