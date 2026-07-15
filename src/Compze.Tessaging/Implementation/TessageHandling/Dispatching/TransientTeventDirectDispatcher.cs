using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Teventive.Tevents.Public;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

///<summary>The receiving half of the transient delivery leg — the direct-dispatch counterpart of the inbox: an arriving transient<br/>
/// tevent is dispatched to this endpoint's subscribed handlers right here, in its own scope and its own transaction, with no store,<br/>
/// no dedup and no retry (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>). Handlers execute before the transport<br/>
/// acknowledgement is written, so one-tessage-in-flight-per-destination keeps handling in send order.</summary>
class TransientTeventDirectDispatcher
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<TransientTeventDirectDispatcher>()
                                     .CreatedBy((ITessageHandlerRegistry handlerRegistry, TeventObservationDispatcher teventObservationDispatcher, IScopeFactory scopeFactory, ITessagesInFlightTracker tessagesInFlightTracker, IBackgroundExceptionReporter exceptionReporter, EndpointConfiguration configuration)
                                                   => new TransientTeventDirectDispatcher(handlerRegistry, teventObservationDispatcher, scopeFactory, tessagesInFlightTracker, exceptionReporter, configuration.Id)));

   readonly ITessageHandlerRegistry _handlerRegistry;
   readonly TeventObservationDispatcher _teventObservationDispatcher;
   readonly IScopeFactory _scopeFactory;
   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly IBackgroundExceptionReporter _exceptionReporter;
   readonly EndpointId _endpointId;

   TransientTeventDirectDispatcher(ITessageHandlerRegistry handlerRegistry, TeventObservationDispatcher teventObservationDispatcher, IScopeFactory scopeFactory, ITessagesInFlightTracker tessagesInFlightTracker, IBackgroundExceptionReporter exceptionReporter, EndpointId endpointId)
   {
      _handlerRegistry = handlerRegistry;
      _teventObservationDispatcher = teventObservationDispatcher;
      _scopeFactory = scopeFactory;
      _tessagesInFlightTracker = tessagesInFlightTracker;
      _exceptionReporter = exceptionReporter;
      _endpointId = endpointId;
   }

   public void Dispatch(TransportTessage.InComing transportTessage)
   {
      this.Log().Debug($"Direct-dispatching {transportTessage.TessageTypeEnum} tessage {transportTessage.TessageId}");
      try
      {
         //The whole wrapped tevent travels the wire, so a received tevent arrives already wrapped; Wrapped normalizes and passes it through unchanged.
         var wrappedTevent = PublisherIdentifyingTevent.Wrapped((ITevent)transportTessage.DeserializeTessageAndCacheForNextCall());
         //Observation fires on arrival, before and outside the transactional handling below.
         _teventObservationDispatcher.Dispatch(wrappedTevent);
         using var scope = _scopeFactory.BeginScope();
         TransactionScopeCe.Execute(() =>
         {
            foreach(var handler in _handlerRegistry.GetTeventHandlers(wrappedTevent.GetType()))
               handler(wrappedTevent, scope.Resolver);
         });
      }
#pragma warning disable CA1031 //The transient tier has no store to retry from: a failed handling is reported and the tevent is gone - never bounced to the sender, whose delivery already succeeded and who has nothing durable to redeliver from.
      catch(Exception exception)
#pragma warning restore CA1031
      {
         _exceptionReporter.ReportException(exception);
      }

      //Quiescence bookkeeping only: a handling failure's surface is the background-exception reporter above, so the tracker is not handed the exception a second time.
      _tessagesInFlightTracker.DoneWith(transportTessage, _endpointId, exception: null);
   }
}
