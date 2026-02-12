using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;

namespace Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;

public class TessageHandlerRegistrarWithDependencyInjectionSupport
{
   public TessageHandlerRegistrarWithDependencyInjectionSupport(ITessageHandlerRegistrar register, LazyCE<IServiceLocator> serviceLocator)
   {
      Register = register;
      ServiceLocator = serviceLocator;
   }

   public ITessageHandlerRegistrar Register { get; }

   LazyCE<IServiceLocator> ServiceLocator { get; }

   public TService Resolve<TService>() where TService : class => ServiceLocator.Value.Resolve<TService>();
}
