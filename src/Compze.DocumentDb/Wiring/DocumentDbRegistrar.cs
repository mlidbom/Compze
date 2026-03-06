using Compze.DocumentDb.Private;
using Compze.Core.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.DependencyInjection.Abstractions;

namespace Compze.DocumentDb.Wiring;

public static class DocumentDbRegistrar
{
   public static DocumentDbRegistrationBuilder DocumentDb(this IComponentRegistrar registrar)
   {
      registrar.Register(Private.DocumentDb.RegisterWith,
                         DocumentDbSession.RegisterWith);

      return new DocumentDbRegistrationBuilder();
   }
}

public class DocumentDbRegistrationBuilder
{
   public DocumentDbRegistrationBuilder HandleDocumentType<TDocument>(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) where TDocument : class
   {
      DocumentDbApi.HandleDocumentType<TDocument>(registrar);
      return this;
   }
}
