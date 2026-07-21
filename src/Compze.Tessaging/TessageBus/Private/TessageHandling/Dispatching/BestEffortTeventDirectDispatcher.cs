using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Engine.Private;
using Compze.Tessaging.Internal.TessagesInFlight;
using Compze.Tessaging.Private.SystemCE.ThreadingCE;
using Compze.Tessaging.Internal.Transport;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Private.Transport;

namespace Compze.Tessaging.TessageBus.Private.TessageHandling.Dispatching;

///<summary>The receiving half of the best-effort delivery leg — the direct-dispatch counterpart of the inbox: an arriving best-effort<br/>
/// tevent is dispatched through the engine's one executor (<see cref="TessageHandlerExecutor"/>) to this endpoint's subscribed<br/>
/// handlers right here, in its own unit of work, with no store,<br/>
/// no dedup and no retry (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>). Handlers execute before the transport<br/>
/// acknowledgement is written, so one-tessage-in-flight-per-destination keeps handling in send order.</summary>
class BestEffortTeventDirectDispatcher
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<BestEffortTeventDirectDispatcher>()
                                     .CreatedBy((TessageHandlerExecutor executor, TeventObservationDispatcher observationDispatcher, ITessagesInFlightTracker tessagesInFlightTracker, IBackgroundExceptionReporter exceptionReporter, EndpointConfiguration configuration)
                                                   => new BestEffortTeventDirectDispatcher(executor, observationDispatcher, tessagesInFlightTracker, exceptionReporter, configuration.Id)));

   readonly TessageHandlerExecutor _executor;
   readonly TeventObservationDispatcher _observationDispatcher;
   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly IBackgroundExceptionReporter _exceptionReporter;
   readonly EndpointId _endpointId;

   BestEffortTeventDirectDispatcher(TessageHandlerExecutor executor, TeventObservationDispatcher observationDispatcher, ITessagesInFlightTracker tessagesInFlightTracker, IBackgroundExceptionReporter exceptionReporter, EndpointId endpointId)
   {
      _executor = executor;
      _observationDispatcher = observationDispatcher;
      _tessagesInFlightTracker = tessagesInFlightTracker;
      _exceptionReporter = exceptionReporter;
      _endpointId = endpointId;
   }

   public async Task DispatchAsync(TransportTessage.InComing transportTessage)
   {
      this.Log().Debug($"Direct-dispatching {transportTessage.TessageTypeEnum} tessage {transportTessage.TessageId}");
      try
      {
         //todo: review: Why would we need to call wrapped here? We should never have anything unwrapped on the protocol, right?
         //The whole wrapped tevent travels the wire, so a received tevent arrives already wrapped; Wrapped normalizes and passes it through unchanged.
         var wrappedTevent = PublisherTevent.Wrapped((ITevent)transportTessage.DeserializeTessageAndCacheForNextCall());
         //An arriving best-effort tevent is already a committed fact on its publisher: it queues for this endpoint's observers on
         //arrival, before the transactional handling below - dispatched off-thread, per-observer FIFO.
         _observationDispatcher.QueueForObservers(wrappedTevent);
         await _executor.ExecuteTeventHandlersInOwnUnitOfWorkAsync(wrappedTevent).caf();
      }
#pragma warning disable CA1031 //The best-effort tier has no store to retry from: a failed handling is reported and the tevent is gone - never bounced to the sender, whose delivery already succeeded and who has nothing durable to redeliver from.
      catch(Exception exception)
#pragma warning restore CA1031
      {
         _exceptionReporter.ReportException(exception);
      }

      //Quiescence bookkeeping only: a handling failure's surface is the background-exception reporter above, so the tracker is not handed the exception a second time.
      _tessagesInFlightTracker.DoneWith(transportTessage, _endpointId, exception: null);
   }
}
