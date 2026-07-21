using Compze.DocumentDb._private;
using Compze.DependencyInjection.Abstractions;

namespace Compze.DocumentDb.Wiring;

public static class DocumentDbRegistrar
{
   public static IComponentRegistrar DocumentDb(this IComponentRegistrar registrar) =>
      registrar.Register(_private.DocumentDb.RegisterWith,
                         DocumentDbSession.RegisterWith);
}
