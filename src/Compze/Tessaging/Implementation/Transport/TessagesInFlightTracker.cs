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
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Tessaging.Implementation.Transport;

class TessagesInFlightTracker(ITypeMapper typeMapper) : ITessagesInFlightTracker
{
   readonly IThreadShared<NonThreadSafeImplementation> _implementation = IThreadShared.WithDefaultTimeout(new NonThreadSafeImplementation(typeMapper));

   public IReadOnlyList<Exception> GetExceptions() => _implementation.Update(implementation => implementation.GetExceptions());

   //performance: Do we care about tueries here? Could we exclude them and lessen the contention a lot?
   public void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId) =>
      _implementation.Update(implementation => implementation.SendingTessageOnTransport(transportTessage, remoteEndpointId));

   public void AwaitNoTessagesInFlight(TimeSpan? timeoutOverride) =>
      _implementation.Await(timeoutOverride ?? 10.Seconds(), implementation => implementation.NoTessagesInFlight());

   public void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception) =>
      _implementation.Update(implementation => implementation.DoneWith(tessage, handlingEndpointId, exception));

   class InFlightTessage
   {
      public Dictionary<EndpointId, bool> EndpointDeliveryStatus { get; } = [];
   }

   class NonThreadSafeImplementation(ITypeMapper typeMapper)
   {
      readonly ITypeMapper _typeMapper = typeMapper;
      internal readonly Dictionary<TessageId, InFlightTessage> TrackedTessages = [];

      readonly List<Exception> _busExceptions = [];

      public IReadOnlyList<Exception> GetExceptions() => _busExceptions.ToList();

      public void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId)
      {
         var inFlightTessage = TrackedTessages.GetOrAdd(transportTessage.TessageId, () => new InFlightTessage());
         inFlightTessage.EndpointDeliveryStatus[remoteEndpointId] = false;
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
   }
}

class NullOpTessagesInFlightTracker : ITessagesInFlightTracker
{
   public IReadOnlyList<Exception> GetExceptions() => [];
   public void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId) {}
   public void AwaitNoTessagesInFlight(TimeSpan? timeoutOverride) {}
   public void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception) {}
}
