using Compze.Must;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.ExactlyOnce;

///<summary>A try of a tessage's handling that is still running as its endpoint stops runs to its end before the endpoint's<br/>
/// inbox has drained, and when that try marks the tessage failed, the testing host's disposal reports the failure, as it<br/>
/// reports one from before the stop. The host is disposed without waiting for its endpoints to be at rest, so that the try is<br/>
/// still running as the Receiver stops: a try of a tessage that the inbox recovered at the endpoint's start, which the at-rest<br/>
/// wait doesn't wait for, runs on into the stop in the same way.</summary>
public class Given_an_exactly_once_tommand_whose_handler_fails_while_its_receiver_stops() : ExactlyOnceSenderAndReceiverSpecification(RetryPolicy.GiveUpAfter(TimeSpan.Zero))
{
   static readonly WaitTimeout HandlingTimeout = WaitTimeout.Seconds(30);
   AggregateException? _hostDisposalException;

   protected override async Task InitializeAsyncInternal()
   {
      TriesOfTheFirstTommandThatFail = int.MaxValue;
      await StartHostAsync();
      TriesGate.Close();
      await SendAsync(1);
      await TriesGate.AwaitQueueLengthEqualToAsync(1, HandlingTimeout);

      var disposal = DisposeHostWithoutWaitingForItsEndpointsToBeAtRestAsync();
      //The try fails only once the Receiver has begun stopping, so only after the host has begun disposing its endpoints.
      await ReceiverStopsGate.AwaitPassedThroughCountEqualToAsync(1, HandlingTimeout);
      TriesGate.Open();
      _hostDisposalException = await disposal;
   }

   [PCT] public void the_tommand_is_tried_once() => TriesOf(1).Count.Must().Be(1);

   [PCT] public void the_tommand_is_not_handled() => HandledTommandNumbers.Must().BeEmpty();

   [PCT] public void disposing_the_host_reports_the_failure_that_marked_the_tommand_failed() =>
      NumbersOfTheTommandsWhoseFailuresAreReportedIn(_hostDisposalException).Must().SequenceEqual([1]);
}
