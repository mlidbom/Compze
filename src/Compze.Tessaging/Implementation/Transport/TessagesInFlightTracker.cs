using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Tessaging.Implementation.Transport;

public class TessagesInFlightTracker(ITypeMapper typeMapper) : ITessagesInFlightTracker
{
   readonly IThreadShared<NonThreadSafeImplementation> _implementation = IThreadShared.WithDefaultTimeouts(new NonThreadSafeImplementation(typeMapper));

   public IReadOnlyList<Exception> GetExceptions() => _implementation.Update(implementation => implementation.GetExceptions());

   //performance: Do we care about tueries here? Could we exclude them and lessen the contention a lot?
   public void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId) =>
      _implementation.Update(implementation => implementation.SendingTessageOnTransport(transportTessage, remoteEndpointId));

   public void AwaitNoTessagesInFlight(TimeSpan? timeoutOverride)
   {
      try
      {
         _implementation.Await(implementation => implementation.NoTessagesInFlight(), timeoutOverride ?? 10.Seconds());
      }
      catch(AwaitingConditionTimeoutException e)
      {
         var diagnosticReport = _implementation.Read(implementation => implementation.CreateDiagnosticReport());
         throw new AwaitingConditionTimeoutException(e, diagnosticReport);
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

      public string CreateDiagnosticReport()
      {
         var sb = new StringBuilder();
         sb.AppendLine("AwaitNoTessagesInFlight timed out.");

         var undeliveredTessages = TrackedTessages.Values
                                                  .Where(t => t.EndpointDeliveryStatus.Values.Any(delivered => !delivered))
                                                  .ToList();

         if(undeliveredTessages.Count > 0)
         {
            sb.AppendLine(CultureInfo.InvariantCulture,
                          $"""

                           ========== UNDELIVERED TESSAGES ({undeliveredTessages.Count}) ==========
                           """);

            foreach(var tessage in undeliveredTessages)
            {
               var pendingEndpoints = string.Join(", ", tessage.EndpointDeliveryStatus.Where(kvp => !kvp.Value).Select(kvp => kvp.Key));
               sb.AppendLine(CultureInfo.InvariantCulture,
                             $"""

                              --- TessageId: {tessage.TessageId} ---
                              Type: {tessage.TypeName}
                              Pending endpoints: {pendingEndpoints}
                              Body:
                              {tessage.Body.Indent()}
                              """);
            }
         } else
         {
            sb.AppendLine("""

                          No undelivered tessages (all tessages were delivered before the diagnostic report was created).
                          """);
         }

         if(_busExceptions.Count > 0)
         {
            sb.AppendLine(CultureInfo.InvariantCulture,
                          $"""

                           ========== BUS EXCEPTIONS ({_busExceptions.Count}) ==========
                           """);

            for(var i = 0; i < _busExceptions.Count; i++)
            {
               sb.AppendLine(CultureInfo.InvariantCulture,
                             $"""

                              --- Exception {i + 1} of {_busExceptions.Count} ---
                              {_busExceptions[i]}
                              """);
            }
         }

         return sb.ToString();
      }
   }
}

public class NullOpTessagesInFlightTracker : ITessagesInFlightTracker
{
   public IReadOnlyList<Exception> GetExceptions() => [];
   public void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId) {}
   public void AwaitNoTessagesInFlight(TimeSpan? timeoutOverride) {}
   public void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception) {}
}
