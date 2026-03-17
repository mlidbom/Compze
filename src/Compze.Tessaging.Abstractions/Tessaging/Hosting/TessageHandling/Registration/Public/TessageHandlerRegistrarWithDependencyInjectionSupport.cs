using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

public class TessageHandlerRegistrarWithDependencyInjectionSupport(ITessageHandlerRegistrar registrar, LazyCE<IServiceLocator> serviceLocator)
{
   internal ITessageHandlerRegistrar Registrar { get; } = registrar;

   LazyCE<IServiceLocator> ServiceLocator { get; } = serviceLocator;

   internal TService Resolve<TService>() where TService : class => ServiceLocator.Value.Resolve<TService>();
}
