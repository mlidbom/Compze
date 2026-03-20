using Autofac.Extensions.DependencyInjection;
using Compze.Underscore;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Autofac.Extensions.Hosting;

public class CompzeAutofacServiceProviderFactory(AutofacContainerBuilder compzeBuilder) : IServiceProviderFactory<global::Autofac.ContainerBuilder>
{
   readonly AutofacContainerBuilder _compzeBuilder = compzeBuilder;

   public global::Autofac.ContainerBuilder CreateBuilder(IServiceCollection services) =>
      ((IAutofacBuilderInternals)_compzeBuilder).ContainerBuilder
                                                ._mutate(it => it.Populate(services));

   public IServiceProvider CreateServiceProvider(global::Autofac.ContainerBuilder containerBuilder)
   {
      var container = _compzeBuilder.Build();
      return new AutofacServiceProvider(((IAutofacContainerInternals)container).Container);
   }
}
