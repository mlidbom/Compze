using Compze.DependencyInjection.Autofac;
using Compze.DependencyInjection.Autofac.Extensions.Hosting;
using Compze.DependencyInjection.Wiring.Registration;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Specification;

namespace Compze.DependencyInjection.MsDiCompliance;

[UsedImplicitly] public class CompzeAutofacMsDiComplianceTests : DependencyInjectionSpecificationTests
{
   static readonly ContainerOptions ComplianceOptions = new() { AllowScopedResolutionFromRoot = true };

   protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
   {
      var builder = new AutofacContainerBuilder();
      var factory = new CompzeAutofacServiceProviderFactory(builder, ComplianceOptions);
      var autofacBuilder = factory.CreateBuilder(serviceCollection);
      return factory.CreateServiceProvider(autofacBuilder);
   }
}
