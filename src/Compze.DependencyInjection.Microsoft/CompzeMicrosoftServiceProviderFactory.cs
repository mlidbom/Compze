using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Microsoft;

public class CompzeMicrosoftServiceProviderFactory(MicrosoftDependencyInjectionContainer compzeContainer) : IServiceProviderFactory<IServiceCollection>
{
   readonly MicrosoftDependencyInjectionContainer _compzeContainer = compzeContainer;

   public IServiceCollection CreateBuilder(IServiceCollection services)
   {
      var compzeServices = ((IMicrosoftContainerInternals)_compzeContainer).ServiceCollection;
      foreach(var descriptor in compzeServices)
         services.Add(descriptor);
      return services;
   }

   public IServiceProvider CreateServiceProvider(IServiceCollection services)
   {
      var innerProvider = services.BuildServiceProvider(new ServiceProviderOptions
      {
         ValidateOnBuild = true,
         ValidateScopes = true
      });

      return new CompzeMicrosoftServiceProvider(_compzeContainer, innerProvider);
   }
}
