using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Threading;
using Compze.Threading.ResourceAccess;
using Compze.Threading.ResourceAccess.Exceptions;

namespace Compze.Tessaging.Implementation.Transport;

public class TessagesInFlightTracker(ITypeMapper typeMapper) : ITessagesInFlightTracker
{
   readonly IAwaitableThreadShared<NonThreadSafeImplementation> _implementation = IAwaitableThreadShared.WithDefaultTimeouts(new NonThreadSafeImplementation(typeMapper));

   public IReadOnlyList<Exception> GetExceptions() => _implementation.Update(implementation => implementation.GetExceptions());

   //performance: Do we care about tueries here? Could we exclude them and lessen the contention a lot?
   public void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId) =>
      _implementation.Update(implementation => implementation.SendingTessageOnTransport(transportTessage, remoteEndpointId));

   public void AwaitNoTessagesInFlight(WaitTimeout? timeoutOverride)
   {
      try
      {
         _implementation.Await(implementation => implementation.NoTessagesInFlight(), timeoutOverride ?? WaitTimeout.Seconds(10));
      }
      catch(AwaitingConditionTimeoutException e)
      {
         throw _implementation.Read(implementation => new AwaitNoTessagesInFlightTimeoutException(innerException: e, undeliveredTessages: implementation.GetUndeliveredTessages()));
      }
   }

   public void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception) =>
      _implementation.Update(implementation => implementation.DoneWith(tessage, handlingEndpointId, exception));

   public class InFlightTessage
   {
      public required TessageId TessageId { get; init; }
      public required string TypeName { get; init; }
      public required string Body { get; init; }
      public Dictionary<EndpointId, bool> EndpointDeliveryStatus { get; } = [];
   }

   public class NonThreadSafeImplementation(ITypeMapper typeMapper)
   {
      readonly ITypeMapper _typeMapper = typeMapper;
      internal readonly Dictionary<TessageId, InFlightTessage> TrackedTessages = [];

      readonly List<Exception> _busExceptions = [];

      public IReadOnlyList<Exception> GetExceptions() => _busExceptions.ToList();

      public void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId)
      {
         var inFlightTessage = TrackedTessages.GetOrAdd(transportTessage.TessageId,
                                                        () => new InFlightTessage
                                                              {
                                                                 TessageId = transportTessage.TessageId,
                                                                 TypeName = _typeMapper.GetType(transportTessage.Type).FullName ?? transportTessage.Type.ToString(),
                                                                 Body = transportTessage.Body
                                                              });

         inFlightTessage.EndpointDeliveryStatus.TryAdd(remoteEndpointId, false); //Retrying messages must not reset the status of already delivered messages.
      }

      public void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception)
      {
         var tessageType = _typeMapper.GetType(tessage.TessageTypeId);
         if(tessageType == typeof(TessageTypesInternal.EndpointInformationTuery))
            return; //this is an initial endpoint information request though which the endpoint IDs we use to track tessages is first established.
         if(exception != null)
         {
            _busExceptions.Add(exception);
         }

         var inFlightTessage = TrackedTessages[tessage.TessageId];
         inFlightTessage.EndpointDeliveryStatus[handlingEndpointId] = true;
      }

      public bool NoTessagesInFlight() => TrackedTessages.Values.SelectMany(it => it.EndpointDeliveryStatus.Values).All(delivered => delivered);

      public IReadOnlyList<InFlightTessage> GetUndeliveredTessages() =>
         TrackedTessages.Values.Where(t => t.EndpointDeliveryStatus.Values.Any(delivered => !delivered)).ToList();
   }
}

internal class NullOpTessagesInFlightTracker : ITessagesInFlightTracker
{
   public IReadOnlyList<Exception> GetExceptions() => [];
   public void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId) {}
   public void AwaitNoTessagesInFlight(WaitTimeout? timeoutOverride) {}
   public void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception) {}
}
