using Compze.DependencyInjection;
using Compze.SystemCE;

namespace Compze.Messaging.Buses;

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