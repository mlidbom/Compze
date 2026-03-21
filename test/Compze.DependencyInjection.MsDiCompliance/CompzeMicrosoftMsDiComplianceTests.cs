using Compze.DependencyInjection.Microsoft;
using Compze.DependencyInjection.Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Specification;

namespace Compze.DependencyInjection.MsDiCompliance;

public class CompzeMicrosoftMsDiComplianceTests : DependencyInjectionSpecificationTests
{
   protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
   {
      var builder = new MicrosoftContainerBuilder();
      var factory = new MicrosoftServiceProviderFactory(builder);
      var mergedCollection = factory.CreateBuilder(serviceCollection);
      return factory.CreateServiceProvider(mergedCollection);
   }
}
