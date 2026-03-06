using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

public class TypermediaHandlerRegistrarWithDependencyInjectionSupport(ITypermediaHandlerRegistrar registrar, LazyCE<IServiceLocator> serviceLocator)
{
   internal ITypermediaHandlerRegistrar Registrar { get; } = registrar;

   LazyCE<IServiceLocator> ServiceLocator { get; } = serviceLocator;

   internal TService Resolve<TService>() where TService : class => ServiceLocator.Value.Resolve<TService>();
}
