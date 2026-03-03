using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Threading;
using Compze.Threading.ResourceAccess;
using Compze.Threading.ResourceAccess.Exceptions;

namespace Compze.Tessaging.Implementation.Transport;

public class TessagesInFlightTracker(ITypeMapper typeMapper) : ITessagesInFlightTracker
{
   readonly IAwaitableThreadShared<NonThreadSafeImplementation> _implementation = IAwaitableThreadShared.New(new NonThreadSafeImplementation(typeMapper));

   public IReadOnlyList<Exception> GetExceptions() => _implementation.Update(it => it.GetExceptions());

   //performance: Do we care about tueries here? Could we exclude them and lessen the contention a lot?
   public void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId) =>
      _implementation.Update(it => it.SendingTessageOnTransport(transportTessage, remoteEndpointId));

   public void AwaitNoTessagesInFlight(WaitTimeout? timeoutOverride)
   {
      try
      {
         _implementation.Await(it => it.NoTessagesInFlight(), timeoutOverride ?? WaitTimeout.Seconds(10));
      }
      catch(AwaitingConditionTimeoutException e)
      {
         throw _implementation.Read(it => new AwaitNoTessagesInFlightTimeoutException(innerException: e, undeliveredTessages: it.GetUndeliveredTessages()));
      }
   }

   public void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception) =>
      _implementation.Update(it => it.DoneWith(tessage, handlingEndpointId, exception));

   public class InFlightTessage
   {
      public required TessageId TessageId { get; init; }
      public required string TypeName { get; init; }
      public required string Body { get; init; }
      internal Dictionary<EndpointId, bool> EndpointDeliveryStatus { get; } = [];
   }

   class NonThreadSafeImplementation(ITypeMapper typeMapper)
   {
      readonly ITypeMapper _typeMapper = typeMapper;
      readonly Dictionary<TessageId, InFlightTessage> _trackedTessages = [];

      readonly List<Exception> _busExceptions = [];

      internal IReadOnlyList<Exception> GetExceptions() => _busExceptions.ToList();

      internal void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId)
      {
         var inFlightTessage = _trackedTessages.GetOrAdd(transportTessage.TessageId,
                                                         () => new InFlightTessage
                                                               {
                                                                  TessageId = transportTessage.TessageId,
                                                                  TypeName = _typeMapper.GetType(transportTessage.Type).FullName ?? transportTessage.Type.ToString(),
                                                                  Body = transportTessage.Body
                                                               });

         inFlightTessage.EndpointDeliveryStatus.TryAdd(remoteEndpointId, false); //Retrying messages must not reset the status of already delivered messages.
      }

      internal void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception)
      {
         var tessageType = _typeMapper.GetType(tessage.TessageTypeId);
         if(tessageType == typeof(TessageTypesInternal.EndpointInformationTuery))
            return; //this is an initial endpoint information request though which the endpoint IDs we use to track tessages is first established.
         if(exception != null)
         {
            _busExceptions.Add(exception);
         }

         var inFlightTessage = _trackedTessages[tessage.TessageId];
         inFlightTessage.EndpointDeliveryStatus[handlingEndpointId] = true;
      }

      internal bool NoTessagesInFlight() => _trackedTessages.Values.SelectMany(it => it.EndpointDeliveryStatus.Values).All(delivered => delivered);

      internal IReadOnlyList<InFlightTessage> GetUndeliveredTessages() =>
         _trackedTessages.Values.Where(t => t.EndpointDeliveryStatus.Values.Any(delivered => !delivered)).ToList();
   }
}

class NullOpTessagesInFlightTracker : ITessagesInFlightTracker
{
   public IReadOnlyList<Exception> GetExceptions() => [];
   public void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId) {}
   public void AwaitNoTessagesInFlight(WaitTimeout? timeoutOverride) {}
   public void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception) {}
}
