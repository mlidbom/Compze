using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

public class TessageHandlerRegistrarWithDependencyInjectionSupport(ITessageHandlerRegistrar tessagingRegistrar, ITypermediaHandlerRegistrar typermediaRegistrar, LazyCE<IServiceLocator> serviceLocator)
{
   internal ITessageHandlerRegistrar TessagingRegistrar { get; } = tessagingRegistrar;
   internal ITypermediaHandlerRegistrar TypermediaRegistrar { get; } = typermediaRegistrar;

   LazyCE<IServiceLocator> ServiceLocator { get; } = serviceLocator;

   internal TService Resolve<TService>() where TService : class => ServiceLocator.Value.Resolve<TService>();
}
