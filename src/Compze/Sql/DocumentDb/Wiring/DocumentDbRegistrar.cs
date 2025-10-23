using Compze.Serialization;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.DocumentDb.Wiring;

public static class DocumentDbRegistrar
{
   public static DocumentDbRegistrationBuilder RegisterDocumentDb(this IEndpointBuilder @this)
      => @this.Container.Register().DocumentDb();

   public static DocumentDbRegistrationBuilder DocumentDb(this IComponentRegistrar registrar)
   {
      registrar.Register(Sql.DocumentDb.DocumentDb.RegisterWith,
                         DocumentDbSerializer.RegisterWith,
                         DocumentDbSession.RegisterWith);

      return new DocumentDbRegistrationBuilder();
   }
}

public class DocumentDbRegistrationBuilder
{
   public DocumentDbRegistrationBuilder HandleDocumentType<TDocument>(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
   {
      DocumentDbApi.HandleDocumentType<TDocument>(registrar);
      return this;
   }
}
