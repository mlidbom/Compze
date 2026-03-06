using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;

namespace Compze.Typermedia.HandlerRegistration;

public class TypermediaHandlerRegistrarWithDependencyInjectionSupport(ITypermediaHandlerRegistrar registrar, LazyCE<IServiceLocator> serviceLocator)
{
   internal ITypermediaHandlerRegistrar Registrar { get; } = registrar;

   LazyCE<IServiceLocator> ServiceLocator { get; } = serviceLocator;

   internal TService Resolve<TService>() where TService : class => ServiceLocator.Value.Resolve<TService>();
}
