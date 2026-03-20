using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Autofac.Extensions.Hosting;

public class CompzeAutofacServiceProviderFactory(AutofacContainerBuilder compzeBuilder) : IServiceProviderFactory<global::Autofac.ContainerBuilder>
{
   readonly AutofacContainerBuilder _compzeBuilder = compzeBuilder;

   public global::Autofac.ContainerBuilder CreateBuilder(IServiceCollection services)
   {
      var builder = ((IAutofacBuilderInternals)_compzeBuilder).ContainerBuilder;
      builder.Populate(services);
      return builder;
   }

   public IServiceProvider CreateServiceProvider(global::Autofac.ContainerBuilder containerBuilder)
   {
      var container = _compzeBuilder.Build();
      return new AutofacServiceProvider(((IAutofacContainerInternals)container).Container);
   }
}
