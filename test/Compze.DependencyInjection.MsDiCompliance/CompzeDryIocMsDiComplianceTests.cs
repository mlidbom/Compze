using Compze.DependencyInjection.DryIoc;
using Compze.DependencyInjection.DryIoc.Extensions.Hosting;
using Compze.DependencyInjection.Wiring.Registration;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Specification;

namespace Compze.DependencyInjection.MsDiCompliance;

[UsedImplicitly] public class CompzeDryIocMsDiComplianceTests : DependencyInjectionSpecificationTests
{
   static readonly ContainerOptions ComplianceOptions = new() { AllowScopedResolutionFromRoot = true };

   protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
   {
      var builder = new DryIocContainerBuilder();
      var factory = new CompzeDryIocServiceProviderFactory(builder, ComplianceOptions);
      var dryIocServiceProvider = factory.CreateBuilder(serviceCollection);
      return factory.CreateServiceProvider(dryIocServiceProvider);
   }
}
