using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Autofac;

public class CompzeAutofacServiceProviderFactory(AutofacDependencyInjectionContainer compzeContainer) : IServiceProviderFactory<ContainerBuilder>
{
   readonly AutofacDependencyInjectionContainer _compzeContainer = compzeContainer;

   public ContainerBuilder CreateBuilder(IServiceCollection services)
   {
      var builder = ((IAutofacContainerInternals)_compzeContainer).ContainerBuilder;
      builder.Populate(services);
      return builder;
   }

   public IServiceProvider CreateServiceProvider(ContainerBuilder containerBuilder)
   {
      _ = _compzeContainer.ServiceLocator;
      return new CompzeAutofacServiceProvider(_compzeContainer);
   }
}
