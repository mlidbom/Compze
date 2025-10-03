using Compze.DependencyInjection;
using Compze.SystemCE;
using Compze.Utilities.SystemCE;

namespace Compze.Tessaging.Buses;

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