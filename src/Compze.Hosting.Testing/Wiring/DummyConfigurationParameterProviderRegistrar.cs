using Compze.Abstractions.Configuration;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Hosting.Testing.Wiring;

public static class DummyConfigurationParameterProviderRegistrar
{
   ///<summary>Registers an <see cref="IConfigurationParameterProvider"/> that throws on every lookup — for tests whose components must never actually read configuration.</summary>
   public static IComponentRegistrar DummyConfigurationParameterProvider(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IConfigurationParameterProvider>()
                                     .CreatedBy(() => new DummyConfigurationParameterProviderCore()));

   class DummyConfigurationParameterProviderCore : IConfigurationParameterProvider
   {
      public string GetString(string parameterName, string? valueIfMissing = null) => throw new NotImplementedException();
   }
}
