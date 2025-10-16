using System;
using Compze.Tessaging.Hosting.Configuration;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.DependencyInjection;

static class DummyConfigurationParameterProviderRegistrar
{
   internal static IComponentRegistrar DummyConfigurationParameterProvider(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IConfigurationParameterProvider>()
                                     .CreatedBy(() => new DummyConfigurationParameterProviderImpl()));

   class DummyConfigurationParameterProviderImpl : IConfigurationParameterProvider
   {
      public string GetString(string parameterName, string? valueIfMissing = null) => throw new NotImplementedException();
   }
}
