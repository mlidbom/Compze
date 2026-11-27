using System.Diagnostics;
using Compze.Must;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.ExactlyOnce;

///<summary>Stopping an endpoint ends the wait of a tessage to be tried again, rather than the stop waiting for the tessage's<br/>
/// retries, and leaves the tessage unhandled. The endpoint's next process tries it again before the tessages behind it.</summary>
public class Given_an_exactly_once_tommand_waiting_to_be_tried_again_when_its_receiver_stops() : ExactlyOnceSenderAndReceiverSpecification(RetryPolicy.RetryForever)
{
   static readonly WaitTimeout HandlingTimeout = WaitTimeout.Seconds(30);
   TimeSpan _timeTheStopTook;
   AggregateException? _hostDisposalException;

   protected override async Task InitializeAsyncInternal()
   {
      TriesOfTheFirstTommandThatFail = int.MaxValue;
      await StartHostAsync();
      await SendAsync(1);
      await SendAsync(2);
      //Three tries at once and one after a second: the first tommand now waits two seconds to be tried again.
      await TriesGate.AwaitPassedThroughCountEqualToAsync(4, HandlingTimeout);

      var stopStartedAt = Stopwatch.GetTimestamp();
      _hostDisposalException = await DisposeHostWithoutWaitingForItsEndpointsToBeAtRestAsync();
      _timeTheStopTook = Stopwatch.GetElapsedTime(stopStartedAt);

      TriesOfTheFirstTommandThatFail = 0;
      await StartHostAsync();
      await HandledGate.AwaitPassedThroughCountEqualToAsync(2, HandlingTimeout);
   }

   //An inbox drain that waited for the retries would take its whole 30 second patience, since the first tommand would never be handled.
   [PCT] public void the_receivers_stop_does_not_wait_for_the_tommand_to_be_tried_again() => _timeTheStopTook.Must().BeLessThan(TimeSpan.FromSeconds(10));

   //The tommand is left unhandled, not marked failed: no failure is reported.
   [PCT] public void disposing_the_host_that_stops_the_receiver_reports_nothing() => _hostDisposalException.Must().BeNull();

   [PCT] public void the_receivers_next_process_handles_the_tommand_before_the_tommand_sent_after_it() => HandledTommandNumbers.Must().SequenceEqual([1, 2]);

   //Four tries before the stop, and the next process's first.
   [PCT] public void the_receiver_makes_no_try_after_its_stop_and_its_next_process_handles_the_tommand_on_its_first_try() => TriesOf(1).Count.Must().Be(5);
}
