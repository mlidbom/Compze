using Compze.DependencyInjection.Wiring.Registration;
using Compze.DocumentDb.Private;

namespace Compze.DocumentDb.Wiring;

public static class DocumentDbRegistrar
{
   public static IComponentRegistrar DocumentDb(this IComponentRegistrar registrar) =>
      registrar.Register(Private.DocumentDb.RegisterWith,
                         DocumentDbSession.RegisterWith);
}
