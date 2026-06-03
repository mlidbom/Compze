using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.LightInject;
using Compze.DependencyInjection.LightInject.Extensions.Hosting;
using JetBrains.Annotations;
using LightInject;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Specification;
using ContainerOptions = Compze.DependencyInjection.Abstractions.ContainerOptions;

namespace Compze.DependencyInjection.MsDiCompliance;

[UsedImplicitly] public class CompzeLightInjectMsDiComplianceTests : DependencyInjectionSpecificationTests
{
   static readonly ContainerOptions ComplianceOptions = new() { AllowScopedResolutionFromRoot = true };

   protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
   {
      var builder = new LightInjectContainerBuilder();
      var factory = new CompzeLightInjectServiceProviderFactory(builder, ComplianceOptions);
      var lightInjectContainer = factory.CreateBuilder(serviceCollection);
      return factory.CreateServiceProvider(lightInjectContainer);
   }
}
