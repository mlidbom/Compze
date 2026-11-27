using System.Diagnostics;
using Compze.Must;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.ExactlyOnce;

///<summary>An exactly-once endpoint handles its exactly-once tessages one at a time, in the order they arrived. A tessage whose<br/>
/// handling fails is tried again, with a growing delay, for as long as the endpoint's <see cref="RetryPolicy"/> allow, and<br/>
/// holds the tessages behind it until it is handled or marked failed.</summary>
public abstract class Given_two_exactly_once_tommands_sent_in_order_whose_first_tommands_handler_fails : ExactlyOnceSenderAndReceiverSpecification
{
   static readonly WaitTimeout HandlingTimeout = WaitTimeout.Seconds(30);

   readonly int _tommandsTheReceiverHandles;
   AggregateException? _hostDisposalException;

   protected Given_two_exactly_once_tommands_sent_in_order_whose_first_tommands_handler_fails(RetryPolicy? receiversRetryPolicy, int triesThatFail, bool theFirstTommandIsHandled) : base(receiversRetryPolicy)
   {
      TriesOfTheFirstTommandThatFail = triesThatFail;
      _tommandsTheReceiverHandles = theFirstTommandIsHandled ? 2 : 1;
   }

   protected override async Task InitializeAsyncInternal()
   {
      await StartHostAsync();
      await SendAsync(1);
      await SendAsync(2);
      await HandledGate.AwaitPassedThroughCountEqualToAsync(_tommandsTheReceiverHandles, HandlingTimeout);
      _hostDisposalException = await DisposeHostAsync();
   }

   protected AggregateException? HostDisposalException => _hostDisposalException;

   public class that_fails_its_first_three_tries_with_the_default_retry_policy()
      : Given_two_exactly_once_tommands_sent_in_order_whose_first_tommands_handler_fails(receiversRetryPolicy: null, triesThatFail: 3, theFirstTommandIsHandled: true)
   {
      [PCT] public void the_first_tommand_is_handled_on_its_fourth_try() => TriesOf(1).Count.Must().Be(4);
      [PCT] public void the_second_tommand_is_handled_after_the_first() => HandledTommandNumbers.Must().SequenceEqual([1, 2]);
      [PCT] public void disposing_the_host_reports_nothing() => HostDisposalException.Must().BeNull();
   }

   public class that_fails_its_first_four_tries_with_a_retry_policy_that_retries_forever()
      : Given_two_exactly_once_tommands_sent_in_order_whose_first_tommands_handler_fails(RetryPolicy.RetryForever, triesThatFail: 4, theFirstTommandIsHandled: true)
   {
      [PCT] public void the_first_tommand_is_handled_on_its_fifth_try() => TriesOf(1).Count.Must().Be(5);
      [PCT] public void the_second_tommand_is_handled_after_the_first() => HandledTommandNumbers.Must().SequenceEqual([1, 2]);
   }

   public class that_keeps_failing_with_a_retry_policy_that_gives_up_after_a_second()
      : Given_two_exactly_once_tommands_sent_in_order_whose_first_tommands_handler_fails(RetryPolicy.GiveUpAfter(TimeSpan.FromSeconds(1)), triesThatFail: int.MaxValue, theFirstTommandIsHandled: false)
   {
      [PCT] public void only_the_second_tommand_is_handled() => HandledTommandNumbers.Must().SequenceEqual([2]);

      //The first try begins as the first tommand's handling starts failing, so a little less than the second can pass between
      //it and the end of the last try: the margin is that little.
      [PCT] public void the_second_tommand_waits_until_the_first_tommands_handling_has_failed_for_a_second() =>
         Stopwatch.GetElapsedTime(TriesOf(1)[0].StartedAt, HandlingOf(2).HandledAt).Must().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(950));

      [PCT] public void the_first_tommand_is_tried_three_times_at_once_and_once_more_as_its_handling_has_failed_for_a_second() =>
         TriesOf(1).Count.Must().Be(4);

      [PCT] public void disposing_the_host_reports_the_failure_that_marked_the_first_tommand_failed() =>
         NumbersOfTheTommandsWhoseFailuresAreReportedIn(HostDisposalException).Must().SequenceEqual([1]);
   }

   public class that_keeps_failing_with_a_retry_policy_that_gives_up_at_once()
      : Given_two_exactly_once_tommands_sent_in_order_whose_first_tommands_handler_fails(RetryPolicy.GiveUpAfter(TimeSpan.Zero), triesThatFail: int.MaxValue, theFirstTommandIsHandled: false)
   {
      [PCT] public void the_first_tommand_is_tried_once() => TriesOf(1).Count.Must().Be(1);
      [PCT] public void only_the_second_tommand_is_handled() => HandledTommandNumbers.Must().SequenceEqual([2]);
   }
}
