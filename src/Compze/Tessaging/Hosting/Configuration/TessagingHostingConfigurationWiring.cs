using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Configuration;

static class TessagingHostingConfigurationWiring
{
   public static IDependencyRegistrar JSonAppConfigFileConfigurationParameterProvider(this IDependencyRegistrar @this)
      => @this.Register(AppSettingsJsonConfigurationParameterProvider.RegisterWith);
}
