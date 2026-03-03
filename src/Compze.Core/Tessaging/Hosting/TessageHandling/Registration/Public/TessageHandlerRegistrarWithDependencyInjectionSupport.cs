using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;

public class TessageHandlerRegistrarWithDependencyInjectionSupport(ITessageHandlerRegistrar register, LazyCE<IServiceLocator> serviceLocator)
{
   internal ITessageHandlerRegistrar Register { get; } = register;

   LazyCE<IServiceLocator> ServiceLocator { get; } = serviceLocator;

   internal TService Resolve<TService>() where TService : class => ServiceLocator.Value.Resolve<TService>();
}
