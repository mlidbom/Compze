using System;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarTestingComponentsRegistrar
{
   public static IComponentRegistrar CurrentTestsPluggableComponents(this IComponentRegistrar register) =>
      register.CurrentTestsPluggableComponents(Guid.NewGuid().ToString());

   public static IComponentRegistrar CurrentTestsPluggableComponents(this IComponentRegistrar register, string connectionStringName) =>
      register.CurrentTestsTransport()
              .CurrentTestsConfiguredSqlLayer(connectionStringName);
}
