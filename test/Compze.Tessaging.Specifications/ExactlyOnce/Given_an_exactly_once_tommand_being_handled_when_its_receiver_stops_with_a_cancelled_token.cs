using System.Diagnostics;
using Compze.Must;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.ExactlyOnce;

///<summary>A stop whose token is cancelled lets go at once: it doesn't drain the inbox, and the try still running is cancelled<br/>
/// through the token its handler was handed. The tommand is left unhandled, not marked failed, though the Receiver's retry<br/>
/// policy gives up at the first failed try: a try cancelled by the stop doesn't count. The Receiver's next process handles it.</summary>
public class Given_an_exactly_once_tommand_being_handled_when_its_receiver_stops_with_a_cancelled_token() : ExactlyOnceSenderAndReceiverSpecification(RetryPolicy.GiveUpAfter(TimeSpan.Zero))
{
   static readonly WaitTimeout HandlingTimeout = WaitTimeout.Seconds(30);
   TimeSpan _timeTheStopTook;
   AggregateException? _hostDisposalException;

   protected override async Task InitializeAsyncInternal()
   {
      TheFirstTommandsFirstTryWaitsForItsCancellation = true;
      await StartHostAsync();
      await SendAsync(1);
      await TriesGate.AwaitPassedThroughCountEqualToAsync(1, HandlingTimeout);

      var stopStartedAt = Stopwatch.GetTimestamp();
      await StopTheReceiverAsync(new CancellationToken(canceled: true));
      _timeTheStopTook = Stopwatch.GetElapsedTime(stopStartedAt);
      _hostDisposalException = await DisposeHostWithoutWaitingForItsEndpointsToBeAtRestAsync();

      await StartHostAsync();
      await HandledGate.AwaitPassedThroughCountEqualToAsync(1, HandlingTimeout);
   }

   //A drain would wait its whole 30 second patience, since the try would wait a minute for its token.
   [PCT] public void the_receivers_stop_does_not_wait_for_the_running_try() => _timeTheStopTook.Must().BeLessThan(TimeSpan.FromSeconds(10));

   [PCT] public void the_running_tries_handler_is_cancelled() => NumbersOfTheTommandsWhoseTriesWereCancelled.Must().SequenceEqual([1]);

   //The tommand is left unhandled, not marked failed: no failure is reported.
   [PCT] public void disposing_the_host_reports_nothing() => _hostDisposalException.Must().BeNull();

   [PCT] public void the_receivers_next_process_handles_the_tommand() => HandledTommandNumbers.Must().SequenceEqual([1]);

   [PCT] public void the_receivers_next_process_handles_the_tommand_on_its_first_try() => TriesOf(1).Count.Must().Be(2);
}
