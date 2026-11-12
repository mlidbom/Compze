using Compze.Contracts;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging._internal.TessagesInFlight;
using Compze.Tessaging._private.SystemCE.ThreadingCE;
using Compze.Tessaging.TessageBus._internal;
using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers;
using Compze.Tessaging._private.Transport;
using Compze.Tessaging._private.Transport.Advertisement;

namespace Compze.Tessaging._private.Routing;

partial class TessagingConnection : ITessagingInboxConnection, IDisposable, IAsyncDisposable
{
   public EndpointInformation EndpointInformation { get; private set; } = null!;

   ///<summary>The address this connection delivers to — the endpoint's location when the connection was made; the endpoint's identity is <see cref="EndpointInformation"/>'s id.</summary>
   internal EndpointAddress RemoteAddress { get; }

   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly ITypeMap _typeMap;
   readonly ITessagingSerializer _serializer;
   readonly ITransportMessagePoster _transportMessagePoster;
   readonly IEndpointInformationQueryTransport _endpointDiscoveryQueryTransport;
   readonly ITaskRunner _taskRunner;
   readonly IBackgroundExceptionReporter _exceptionReporter;

   readonly ExactlyOnceDeliveryStream? _exactlyOnceStream;
   readonly BestEffortDeliveryStream _bestEffortStream;
   readonly CancellationTokenSource _cancellationSource = new();
   bool _deliveryRunning;

   internal TessagingConnection(ITessagesInFlightTracker tessagesInFlightTracker,
                                EndpointAddress remoteAddress,
                                ITypeMap typeMap,
                                ITessagingSerializer serializer,
                                ITransportMessagePoster transportMessagePoster,
                                IEndpointInformationQueryTransport endpointDiscoveryQueryTransport,
                                BestEffortDeliveryStream.Factory bestEffortStreamFactory,
                                ExactlyOnceDeliveryStream.Factory? exactlyOnceStreamFactory,
                                ITaskRunner taskRunner,
                                IBackgroundExceptionReporter exceptionReporter)
   {
      _tessagesInFlightTracker = tessagesInFlightTracker;
      RemoteAddress = remoteAddress;
      _typeMap = typeMap;
      _serializer = serializer;
      _transportMessagePoster = transportMessagePoster;
      _endpointDiscoveryQueryTransport = endpointDiscoveryQueryTransport;
      _taskRunner = taskRunner;
      _exceptionReporter = exceptionReporter;

      //Which delivery streams a connection carries mirrors which delivery machinery the endpoint wires: the best-effort stream -
      //draining the peer's queue in the endpoint's best-effort delivery wiring - is intrinsic to every connection, while the
      //exactly-once stream exists exactly when the outbox's wiring granted the router the factory carrying the storage that backs it.
      _bestEffortStream = bestEffortStreamFactory.CreateFor(this);
      _exactlyOnceStream = exactlyOnceStreamFactory?.CreateFor(this);
   }

   public async Task InitAsync() =>
      EndpointInformation = await _endpointDiscoveryQueryTransport.GetAsync(new EndpointInformationQuery(), RemoteAddress).caf();

   public void EnqueueForExactlyOnceDelivery(ITessage tessage, TessageId dedupId, long deliveryStreamSequenceNumber)
   {
      State.Assert(_exactlyOnceStream is not null, () => "An exactly-once delivery reached a connection that carries no exactly-once stream. Only the outbox sends exactly-once, and the wiring that registers the outbox is the wiring that grants every connection its exactly-once stream — this endpoint wires neither.");

      var transportTessage = TransportTessage.OutGoing.Create(tessage, dedupId, _typeMap, _serializer, deliveryStreamSequenceNumber);
      //Idempotent per (tessage, endpoint), so when the stream's sequence-keyed queue collapses the one legitimate
      //double-enqueue — a commit hook and the recovery backlog load both offering the same tessage — the tracker needs no
      //correction: the surviving entry is delivered and marked done exactly once.
      _tessagesInFlightTracker.SendingTessageOnTransport(transportTessage, EndpointInformation.Id);
      _exactlyOnceStream!.Enqueue(transportTessage);
   }

   public void StartDelivery()
   {
      _deliveryRunning = true;
      _exactlyOnceStream?.Start();
      _bestEffortStream.Start();
   }

   ///<summary>Stops this connection being the peer's delivery path and tells its send loops to stop, without waiting for them to<br/>
   /// end: brief, so the router does it while holding its monitor. <see cref="AwaitDeliveryStoppedAsync"/> waits for the loops.</summary>
   public void BeginStoppingDelivery()
   {
      if(!_deliveryRunning) return;

      _deliveryRunning = false;
      this.Log().Info($"Stopping delivery to endpoint {EndpointInformation.Id}...");
      //Before the cancellation, and before the loops are joined: this connection stops being the peer's delivery path here, so
      //here is where its queue must stop accepting - not whenever the send loop's thread gets round to noticing it was cancelled.
      _bestEffortStream.DetachFromThePeersQueue();
      _cancellationSource.Cancel();
   }

   ///<summary>Completes once the send loops <see cref="BeginStoppingDelivery"/> told to stop have ended, holding no thread while<br/>
   /// they wind down. Throws a <see cref="TimeoutException"/> if a loop takes longer than 5 seconds to end.</summary>
   public async Task AwaitDeliveryStoppedAsync()
   {
      if(_exactlyOnceStream is not null) await _exactlyOnceStream.AwaitSendLoopTerminationAsync().caf();
      await _bestEffortStream.AwaitSendLoopTerminationAsync().caf();
   }

   public async ValueTask DisposeAsync()
   {
      BeginStoppingDelivery();
      await AwaitDeliveryStoppedAsync().caf();
      ReleaseResources();
   }

   ///<summary>For the router's paths that dispose a connection while holding its monitor: a connection whose delivery never<br/>
   /// started, or one that already stopped, has no send loop to wait for, so only the replacement of a live connection blocks.</summary>
   public void Dispose()
   {
      BeginStoppingDelivery();
      AwaitDeliveryStoppedAsync().WaitUnwrappingException();
      ReleaseResources();
   }

   void ReleaseResources()
   {
      _cancellationSource.Dispose();
      _exactlyOnceStream?.Dispose();
   }
}
