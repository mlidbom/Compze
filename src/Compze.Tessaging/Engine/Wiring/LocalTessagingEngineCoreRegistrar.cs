using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Engine.HandlerRegistration;
using Compze.Tessaging.Engine.HandlerRegistration.TessageHandlers;
using Compze.Tessaging.Internals.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.TypeIdentifiers;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Tessaging.Engine.Wiring;

///<summary>Registers the engine's core into a container: the gathered <see cref="TessageHandlerRegistrations"/>, the<br/>
/// <see cref="TessageHandlerRoster"/> built from them when the container's singletons materialize, the one<br/>
/// <see cref="TessageHandlerExecutor"/>, the <see cref="TeventObservationDispatcher"/> — the engine's only background<br/>
/// machinery — and the task runner and background-exception reporter it dispatches through. The one wiring home both the<br/>
/// plain-container composition (<see cref="LocalTessagingEngineRegistrar.LocalTessagingEngine"/>) and the endpoint<br/>
/// compositions (<c>EndpointBuilder</c> in Compze.Tessaging.Endpoints) compose.</summary>
///<remarks>The composition must already have declared its <see cref="ITessagesInFlightTracker"/> when this registers: an<br/>
/// endpoint declares the tracker its testing host hands it — or the null device when nothing tracks — and the plain-container<br/>
/// composition declares the null device, since no testing host exists to await a plain container's at-rest.</remarks>
static class LocalTessagingEngineCoreRegistrar
{
   internal static IComponentRegistrar RegisterLocalTessagingEngineCore(this IComponentRegistrar registrar, TessageHandlerRegistrations handlerRegistrations)
   {
      //The roster is built over the tessage type hierarchy, and the tessages it routes carry entity ids in their payloads.
      return registrar
            .RegisterTypeMap()
            .Register(Singleton.For<TessageHandlerRegistrations>().Instance(handlerRegistrations),
                      Singleton.For<TessageHandlerRoster>().CreatedBy((TessageHandlerRegistrations registrations, ITypeMap typeMap) => registrations.BuildRoster(typeMap)),
                      Singleton.For<TessageHandlerExecutor>().CreatedBy((TessageHandlerRoster roster, IScopeFactory scopeFactory) => new TessageHandlerExecutor(roster, scopeFactory)),
                      Singleton.For<TeventObservationDispatcher>().CreatedBy((TessageHandlerRoster roster, IScopeFactory scopeFactory, IBackgroundExceptionReporter backgroundExceptionReporter, ITessagesInFlightTracker tessagesInFlightTracker, ITaskRunner taskRunner)
                                                                                => new TeventObservationDispatcher(roster, scopeFactory, backgroundExceptionReporter, tessagesInFlightTracker, taskRunner)))
            .TaskRunner()
            .BackgroundExceptionReporter();
   }
}
