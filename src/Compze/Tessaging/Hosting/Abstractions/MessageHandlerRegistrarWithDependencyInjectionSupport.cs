using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;

namespace Compze.Tessaging.Hosting.Abstractions;

public class MessageHandlerRegistrarWithDependencyInjectionSupport
{
   internal MessageHandlerRegistrarWithDependencyInjectionSupport(IMessageHandlerRegistrar register, OptimizedLazy<IServiceLocator> serviceLocator)
   {
      Register = register;
      ServiceLocator = serviceLocator;
   }

   internal IMessageHandlerRegistrar Register { get; }

   OptimizedLazy<IServiceLocator> ServiceLocator { get; }

   internal TService Resolve<TService>() where TService : class => ServiceLocator.Value.Resolve<TService>();
}
