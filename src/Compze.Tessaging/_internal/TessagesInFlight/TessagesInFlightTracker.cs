using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging._private.TessagesInFlight;
using Compze.Tessaging._private.Transport;
using Compze.Tessaging._private.Transport.Advertisement;
using Compze.Threading;
using Compze.Threading.Exceptions;
using Compze.Threading.ResourceAccess;

namespace Compze.Tessaging._internal.TessagesInFlight;

class TessagesInFlightTracker : ITessagesInFlightTracker
{
   readonly IAwaitableThreadShared<NonThreadSafeImplementation> _implementation = IAwaitableThreadShared.New(new NonThreadSafeImplementation());

   public IReadOnlyList<Exception> GetExceptions() => _implementation.Update(it => it.GetExceptions());

   //performance: Do we care about tueries here? Could we exclude them and lessen the contention a lot?
   public void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId) =>
      _implementation.Update(it => it.SendingTessageOnTransport(transportTessage, remoteEndpointId));

   public void AwaitNoTessagesInFlight(WaitTimeout? timeoutOverride)
   {
      try
      {
         _implementation.Await(it => it.NoTessagesInFlight(), timeout: timeoutOverride ?? WaitTimeout.Seconds(10));
      }
      catch(AwaitingConditionTimeoutException e)
      {
         throw _implementation.Read(it => new AwaitNoTessagesInFlightTimeoutException(innerException: e, undeliveredTessages: it.GetUndeliveredTessages(), pendingObservations: it.GetPendingObservations()));
      }
   }

   public void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception) =>
      _implementation.Update(it => it.DoneWith(tessage, handlingEndpointId, exception));

   public void DroppedBeforeDelivery(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId) =>
      _implementation.Update(it => it.DroppedBeforeDelivery(transportTessage, remoteEndpointId));

   public void TeventObservationQueued(Type wrapperTeventType) => _implementation.Update(it => it.TeventObservationQueued(wrapperTeventType));

   public void TeventObservationDispatched(Type wrapperTeventType) => _implementation.Update(it => it.TeventObservationDispatched(wrapperTeventType));

   public class InFlightTessage
   {
      public required TessageId TessageId { get; init; }
      public required string TypeName { get; init; }
      public required string Body { get; init; }
      internal Dictionary<EndpointId, bool> EndpointDeliveryStatus { get; } = [];
   }

   class NonThreadSafeImplementation
   {
      readonly Dictionary<TessageId, InFlightTessage> _trackedTessages = [];

      readonly Dictionary<Type, int> _pendingObservationsByWrapperTeventType = [];

      readonly List<Exception> _busExceptions = [];

      internal IReadOnlyList<Exception> GetExceptions() => [.._busExceptions];

      internal void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId)
      {
         var inFlightTessage = _trackedTessages.GetOrAdd(transportTessage.TessageId,
                                                         () => new InFlightTessage
                                                               {
                                                                  TessageId = transportTessage.TessageId,
                                                                  TypeName = transportTessage.Type.CanonicalString,
                                                                  Body = transportTessage.Body
                                                               });

         inFlightTessage.EndpointDeliveryStatus.TryAdd(remoteEndpointId, false); //Retrying messages must not reset the status of already delivered messages.
      }

      internal void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception)
      {
         if(tessage.TessageTypeId.Type == typeof(EndpointInformationQuery))
            return; //this is an initial endpoint information request though which the endpoint IDs we use to track tessages is first established.
         if(exception != null)
         {
            _busExceptions.Add(exception);
         }

         var inFlightTessage = _trackedTessages[tessage.TessageId];
         inFlightTessage.EndpointDeliveryStatus[handlingEndpointId] = true;
      }

      internal void DroppedBeforeDelivery(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId) =>
         _trackedTessages[transportTessage.TessageId].EndpointDeliveryStatus.Remove(remoteEndpointId);

      internal void TeventObservationQueued(Type wrapperTeventType) =>
         _pendingObservationsByWrapperTeventType[wrapperTeventType] = _pendingObservationsByWrapperTeventType.GetValueOrDefault(wrapperTeventType) + 1;

      internal void TeventObservationDispatched(Type wrapperTeventType)
      {
         var remaining = _pendingObservationsByWrapperTeventType[wrapperTeventType] - 1;
         if(remaining == 0)
            _pendingObservationsByWrapperTeventType.Remove(wrapperTeventType);
         else
            _pendingObservationsByWrapperTeventType[wrapperTeventType] = remaining;
      }

      internal bool NoTessagesInFlight() => _pendingObservationsByWrapperTeventType.Count == 0
                                         && _trackedTessages.Values.SelectMany(it => it.EndpointDeliveryStatus.Values).All(delivered => delivered);

      internal IReadOnlyList<InFlightTessage> GetUndeliveredTessages() =>
         [.._trackedTessages.Values.Where(it => it.EndpointDeliveryStatus.Values.Any(delivered => !delivered))];

      internal IReadOnlyDictionary<string, int> GetPendingObservations() =>
         _pendingObservationsByWrapperTeventType.ToDictionary(it => it.Key.FullName ?? it.Key.Name, it => it.Value);
   }
}
