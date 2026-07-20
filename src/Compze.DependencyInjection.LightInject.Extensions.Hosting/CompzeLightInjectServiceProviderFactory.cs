using Compze.Internals.SystemCE;
using LightInject;
using LightInject.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using ContainerOptions = Compze.DependencyInjection.Abstractions.ContainerOptions;

namespace Compze.DependencyInjection.LightInject.Extensions.Hosting;

public class CompzeLightInjectServiceProviderFactory(LightInjectContainerBuilder builder, ContainerOptions? options = null) : IServiceProviderFactory<IServiceContainer>
{
   readonly LightInjectContainerBuilder _builder = builder;
   readonly ContainerOptions _options = options ?? ContainerOptions.Default;
   IServiceCollection? _services;

   public IServiceContainer CreateBuilder(IServiceCollection services)
   {
      _services = services;
      var container = (ServiceContainer)_builder.CastTo<ILightInjectBuilderInternals>().ServiceContainer;
      container.ConstructorDependencySelector = new AnnotatedConstructorDependencySelector();
      container.ConstructorSelector = new AnnotatedConstructorSelector(container.CanGetInstance);
      return container;
   }

   public IServiceProvider CreateServiceProvider(IServiceContainer containerBuilder)
   {
      _builder.Build(_options);
      return containerBuilder.CreateServiceProvider(_services!);
   }
}
