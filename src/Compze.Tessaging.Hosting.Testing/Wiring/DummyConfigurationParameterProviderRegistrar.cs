using Compze.Core.Configuration.Internal;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

static class DummyConfigurationParameterProviderRegistrar
{
   public static IComponentRegistrar DummyConfigurationParameterProvider(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IConfigurationParameterProvider>()
                                     .CreatedBy(() => new DummyConfigurationParameterProviderCore()));

   class DummyConfigurationParameterProviderCore : IConfigurationParameterProvider
   {
      public string GetString(string parameterName, string? valueIfMissing = null) => throw new NotImplementedException();
   }
}
