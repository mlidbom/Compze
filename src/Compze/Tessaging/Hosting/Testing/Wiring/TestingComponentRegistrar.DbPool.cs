using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class TestingComponentRegistrar_DbPool
{
   public static IComponentRegistrar CurrentTestsDbPoolIfNotAlreadyRegistered(this IComponentRegistrar register) => 
      register.CastTo<TestingComponentRegistrar>().CurrentTestsDbPoolIfNotAlreadyRegistered();
}
