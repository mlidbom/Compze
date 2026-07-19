using Compze.DependencyInjection.Microsoft;
using Compze.DependencyInjection.Microsoft.Extensions.Hosting;
using Compze.DependencyInjection.Wiring.Registration;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Specification;

namespace Compze.DependencyInjection.MsDiCompliance;

[UsedImplicitly] public class CompzeMicrosoftMsDiComplianceTests : DependencyInjectionSpecificationTests
{
   static readonly ContainerOptions ComplianceOptions = new() { AllowScopedResolutionFromRoot = true };

   protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
   {
      var builder = new MicrosoftContainerBuilder();
      var factory = new MicrosoftServiceProviderFactory(builder, ComplianceOptions);
      var mergedCollection = factory.CreateBuilder(serviceCollection);
      return factory.CreateServiceProvider(mergedCollection);
   }
}
