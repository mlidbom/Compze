using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;

namespace Compze.Tessaging.Hosting.Testing.Sql;

public static class DbPoolRegistrar
{
   public static IComponentRegistrar CurrentTestsDbPoolIfNotAlreadyRegistered(this IComponentRegistrar register) => 
      register.CastTo<TestingComponentRegistrar>().CurrentTestsDbPoolIfNotAlreadyRegistered();
}
