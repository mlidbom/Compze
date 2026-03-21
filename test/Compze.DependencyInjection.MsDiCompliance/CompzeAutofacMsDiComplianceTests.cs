using Compze.DependencyInjection.Autofac;
using Compze.DependencyInjection.Autofac.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Specification;

namespace Compze.DependencyInjection.MsDiCompliance;

public class CompzeAutofacMsDiComplianceTests : DependencyInjectionSpecificationTests
{
   protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
   {
      var builder = new AutofacContainerBuilder();
      var factory = new CompzeAutofacServiceProviderFactory(builder);
      var autofacBuilder = factory.CreateBuilder(serviceCollection);
      return factory.CreateServiceProvider(autofacBuilder);
   }
}
