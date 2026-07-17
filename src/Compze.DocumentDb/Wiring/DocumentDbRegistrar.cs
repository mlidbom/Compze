using Compze.DocumentDb.Private;
using Compze.DependencyInjection.Abstractions;

namespace Compze.DocumentDb.Wiring;

public static class DocumentDbRegistrar
{
   public static IComponentRegistrar DocumentDb(this IComponentRegistrar registrar) =>
      registrar.Register(Private.DocumentDb.RegisterWith,
                         DocumentDbSession.RegisterWith);
}
