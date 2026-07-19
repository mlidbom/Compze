using Autofac.Extensions.DependencyInjection;
using Compze.DependencyInjection.Wiring.Registration;
using Compze.Internals.SystemCE;
using Compze.Underscore;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Autofac.Extensions.Hosting;

public class CompzeAutofacServiceProviderFactory(AutofacContainerBuilder builder, ContainerOptions? options = null) : IServiceProviderFactory<global::Autofac.ContainerBuilder>
{
   readonly AutofacContainerBuilder _builder = builder;
   readonly ContainerOptions _options = options ?? ContainerOptions.Default;

   public global::Autofac.ContainerBuilder CreateBuilder(IServiceCollection services) =>
      _builder.CastTo<IAutofacBuilderInternals>()
              .ContainerBuilder
              ._mutate(it => it.Populate(services));

   public IServiceProvider CreateServiceProvider(global::Autofac.ContainerBuilder containerBuilder) =>
      _builder.Build(_options)
              .CastTo<IAutofacContainerInternals>()
              ._(it => new AutofacServiceProvider(it.Container));
}
