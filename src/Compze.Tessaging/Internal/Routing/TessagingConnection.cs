using Compze.Contracts;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.TessageBus.Internal.BestEffortDelivery;
using Compze.Tessaging.TessageBus.Internal.Outbox;
using Compze.Tessaging.Internal.TessagesInFlight;
using Compze.Tessaging.Internal.SystemCE.ThreadingCE;
using Compze.Tessaging.Internal.Transport;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageBus.Internal;
using Compze.Tessaging.Internal.Transport.Advertisement;
using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Internal.Routing;

partial class TessagingConnection : ITessagingInboxConnection, IDisposable
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

   public void EnqueueForExactlyOnceDelivery(ITessage tessage, TessageId dedupId)
   {
      State.Assert(_exactlyOnceStream is not null, () => "An exactly-once delivery reached a connection that carries no exactly-once stream. Only the outbox sends exactly-once, and the wiring that registers the outbox is the wiring that grants every connection its exactly-once stream — this endpoint wires neither.");

      var transportTessage = TransportTessage.OutGoing.Create(tessage, dedupId, _typeMap, _serializer);
      _tessagesInFlightTracker.SendingTessageOnTransport(transportTessage, EndpointInformation.Id);
      _exactlyOnceStream!.Enqueue(transportTessage);
   }

   public void StartDelivery()
   {
      _deliveryRunning = true;
      _exactlyOnceStream?.Start();
      _bestEffortStream.Start();
   }

   public void StopDelivery()
   {
      if(!_deliveryRunning) return;

      _deliveryRunning = false;
      this.Log().Info($"Stopping delivery to endpoint {EndpointInformation.Id}...");
      _cancellationSource.Cancel();
      _exactlyOnceStream?.AwaitSendLoopTermination();
      _bestEffortStream.AwaitSendLoopTermination();
   }

   public void Dispose()
   {
      StopDelivery();
      _cancellationSource.Dispose();
      _exactlyOnceStream?.Dispose();
   }
}
