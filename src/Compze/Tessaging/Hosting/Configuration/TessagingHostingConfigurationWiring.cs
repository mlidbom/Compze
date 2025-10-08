using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Configuration;

static class TessagingHostingConfigurationWiring
{
   public static IDependencyInjectionContainer RegisterConfigFileReading(this IDependencyInjectionContainer @this)
      => @this.Register(Singleton.For<IConfigurationParameterProvider>()
                                 .CreatedBy(() => new AppSettingsJsonConfigurationParameterProvider()));
}
