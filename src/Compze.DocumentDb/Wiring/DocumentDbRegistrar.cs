using Compze.DocumentDb.Private;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.DependencyInjection.Abstractions;
using Compze.Typermedia;

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
   public DocumentDbRegistrationBuilder HandleDocumentType<TDocument>(TessageHandlerRegistrarWithDependencyInjectionSupport tessagingRegistrar, TypermediaHandlerRegistrarWithDependencyInjectionSupport typermediaRegistrar) where TDocument : class
   {
      DocumentDbApi.HandleDocumentType<TDocument>(tessagingRegistrar, typermediaRegistrar);
      return this;
   }
}
