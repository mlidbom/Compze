using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Teventive.Tevents.Public;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

static class TeventObservationDispatcherRegistrar
{
   public static IComponentRegistrar TeventObservationDispatcher(this IComponentRegistrar registrar)
      => registrar.Register(Dispatching.TeventObservationDispatcher.RegisterWith);
}

///<summary>Dispatches a tevent to the endpoint's transaction-ignoring tevent handlers — observation, the bottom rung of the<br/>
/// delivery ladder (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>): direct invocation, once, immediately, in a<br/>
/// fresh scope with any ambient transaction suppressed. Invoked at every point a tevent is first registered — a local publish, an<br/>
/// exactly-once tevent's inbox registration, a transient tevent's arrival.</summary>
///<remarks>The fresh scope keeps an observer's resolutions out of the triggering transaction: were observers handed the publisher's<br/>
/// scope, a scoped database session they resolve could be enlisted in the very transaction observation exists to be undeterred by.<br/>
/// A throwing observer is reported through the <see cref="IBackgroundExceptionReporter"/>, never retried — and never aborts the<br/>
/// remaining observers or the publish/arrival that triggered the dispatch.</remarks>
class TeventObservationDispatcher
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<TeventObservationDispatcher>()
                                     .CreatedBy((ITessageHandlerRegistry handlerRegistry, IScopeFactory scopeFactory, IBackgroundExceptionReporter exceptionReporter)
                                                   => new TeventObservationDispatcher(handlerRegistry, scopeFactory, exceptionReporter)));

   readonly ITessageHandlerRegistry _handlerRegistry;
   readonly IScopeFactory _scopeFactory;
   readonly IBackgroundExceptionReporter _exceptionReporter;

   TeventObservationDispatcher(ITessageHandlerRegistry handlerRegistry, IScopeFactory scopeFactory, IBackgroundExceptionReporter exceptionReporter)
   {
      _handlerRegistry = handlerRegistry;
      _scopeFactory = scopeFactory;
      _exceptionReporter = exceptionReporter;
   }

   public void Dispatch(IPublisherTevent<ITevent> wrappedTevent)
   {
      var observers = _handlerRegistry.GetTransactionIgnoringTeventHandlers(wrappedTevent.GetType());
      if(observers.Count == 0) return;

      //Outside any ambient transaction: observation is the rung that trades transactional coupling away — a locally published
      //tevent's observers fire even if the publisher's transaction later rolls back, so they must not enlist in it.
      TransactionScopeCe.SuppressAmbient(() =>
      {
         using var scope = _scopeFactory.BeginScope();
         foreach(var observer in observers)
         {
            try
            {
               observer(wrappedTevent, scope.Resolver);
            }
#pragma warning disable CA1031 //A throwing observer is reported, never retried - and must not abort the remaining observers or the publish/arrival that triggered the dispatch.
            catch(Exception exception)
#pragma warning restore CA1031
            {
               _exceptionReporter.ReportException(exception);
            }
         }
      });
   }

   ///<summary>Observation of an arriving tessage, deserialization-frugal: an arriving tessage nothing observes is never<br/>
   /// deserialized here (the wrapper type on the envelope answers whether observers match).</summary>
   public void Dispatch(TransportTessage.InComing transportTessage)
   {
      if(_handlerRegistry.GetTransactionIgnoringTeventHandlers(transportTessage.TessageTypeId.Type).Count == 0) return;
      Dispatch(PublisherTevent.Wrapped((ITevent)transportTessage.DeserializeTessageAndCacheForNextCall()));
   }
}
