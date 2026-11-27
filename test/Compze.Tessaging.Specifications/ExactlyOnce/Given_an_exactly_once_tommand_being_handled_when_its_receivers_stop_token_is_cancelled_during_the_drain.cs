using System.Diagnostics;
using Compze.Must;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.ExactlyOnce;

///<summary>A stop drains its inbox until its token is cancelled: the try still running goes on until then, and is cancelled<br/>
/// then, through the token its handler was handed, and the stop returns. The Receiver's next process handles the tommand.</summary>
public class Given_an_exactly_once_tommand_being_handled_when_its_receivers_stop_token_is_cancelled_during_the_drain() : ExactlyOnceSenderAndReceiverSpecification(RetryPolicy.GiveUpAfter(TimeSpan.Zero))
{
   static readonly WaitTimeout HandlingTimeout = WaitTimeout.Seconds(30);
   bool _theStopHadReturnedBeforeItsTokenWasCancelled;
   IReadOnlyList<int> _tommandsWhoseTriesWereCancelledBeforeTheStopsTokenWas = [];
   TimeSpan _timeTheStopTookOnceItsTokenWasCancelled;

   protected override async Task InitializeAsyncInternal()
   {
      TheFirstTommandsFirstTryWaitsForItsCancellation = true;
      await StartHostAsync();
      await SendAsync(1);
      await TriesGate.AwaitPassedThroughCountEqualToAsync(1, HandlingTimeout);

      using var stopsToken = new CancellationTokenSource();
      var stop = StopTheReceiverAsync(stopsToken.Token);
      await AwaitTheReceiverHasBegunStoppingAsync(HandlingTimeout);
      await Task.Delay(TimeSpan.FromMilliseconds(500));
      _theStopHadReturnedBeforeItsTokenWasCancelled = stop.IsCompleted;
      _tommandsWhoseTriesWereCancelledBeforeTheStopsTokenWas = NumbersOfTheTommandsWhoseTriesWereCancelled;

      var cancelledAt = Stopwatch.GetTimestamp();
      await stopsToken.CancelAsync();
      await stop;
      _timeTheStopTookOnceItsTokenWasCancelled = Stopwatch.GetElapsedTime(cancelledAt);
      await DisposeHostWithoutWaitingForItsEndpointsToBeAtRestAsync();

      await StartHostAsync();
      await HandledGate.AwaitPassedThroughCountEqualToAsync(1, HandlingTimeout);
   }

   [PCT] public void the_stop_waits_for_the_running_try_until_its_token_is_cancelled() => _theStopHadReturnedBeforeItsTokenWasCancelled.Must().BeFalse();

   [PCT] public void the_running_try_is_not_cancelled_before_the_stops_token_is() => _tommandsWhoseTriesWereCancelledBeforeTheStopsTokenWas.Must().BeEmpty();

   [PCT] public void the_running_try_is_cancelled_once_the_stops_token_is() => NumbersOfTheTommandsWhoseTriesWereCancelled.Must().SequenceEqual([1]);

   [PCT] public void the_stop_returns_promptly_once_its_token_is_cancelled() => _timeTheStopTookOnceItsTokenWasCancelled.Must().BeLessThan(TimeSpan.FromSeconds(10));

   [PCT] public void the_receivers_next_process_handles_the_tommand() => HandledTommandNumbers.Must().SequenceEqual([1]);
}
