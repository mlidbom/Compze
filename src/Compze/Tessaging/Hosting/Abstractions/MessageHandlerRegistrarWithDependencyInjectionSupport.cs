using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;

namespace Compze.Tessaging.Hosting.Abstractions;

public class MessageHandlerRegistrarWithDependencyInjectionSupport
{
   internal MessageHandlerRegistrarWithDependencyInjectionSupport(IMessageHandlerRegistrar register, LazyCE<IServiceLocator> serviceLocator)
   {
      Register = register;
      ServiceLocator = serviceLocator;
   }

   internal IMessageHandlerRegistrar Register { get; }

   LazyCE<IServiceLocator> ServiceLocator { get; }

   internal TService Resolve<TService>() where TService : class => ServiceLocator.Value.Resolve<TService>();
}
