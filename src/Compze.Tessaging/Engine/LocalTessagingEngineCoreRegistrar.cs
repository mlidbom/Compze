using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Tessaging.TessageHandling.Registration.Public;
using Compze.Tessaging.Typermedia.HandlerRegistration;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Engine;

///<summary>Registers the engine's core into a container: the gathered <see cref="TessageHandlerRegistrations"/> (exposed, for<br/>
/// now, under the per-style registrar interfaces), the <see cref="TessageHandlerRoster"/> built from them when the container's<br/>
/// singletons materialize, the one <see cref="TessageHandlerExecutor"/>, and the background-exception reporter observation<br/>
/// depends on. The one wiring home both the plain-container composition<br/>
/// (<see cref="LocalTessagingEngineRegistrar.LocalTessagingEngine"/>) and the endpoint features<br/>
/// (<see cref="LocalTessagingEngineFeature"/>) compose.</summary>
static class LocalTessagingEngineCoreRegistrar
{
   internal static IComponentRegistrar RegisterLocalTessagingEngineCore(this IComponentRegistrar registrar, TessageHandlerRegistrations handlerRegistrations)
   {
      return registrar.Register(Singleton.For<TessageHandlerRegistrations, ITessageHandlerRegistrar, ITransactionIgnoringTeventHandlerRegistrar, ITypermediaHandlerRegistrar>().Instance(handlerRegistrations),
                         Singleton.For<TessageHandlerRoster>().CreatedBy((TessageHandlerRegistrations registrations, ITypeMap typeMap) => registrations.BuildRoster(typeMap)),
                         Singleton.For<TessageHandlerExecutor>().CreatedBy((TessageHandlerRoster roster, IScopeFactory scopeFactory, IBackgroundExceptionReporter backgroundExceptionReporter)
                                                                             => new TessageHandlerExecutor(roster, scopeFactory, backgroundExceptionReporter)))
                      .BackgroundExceptionReporter();
   }
}
