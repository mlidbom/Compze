using Compze.DocumentDb.Private;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Typermedia.HandlerRegistration;

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
   public DocumentDbRegistrationBuilder HandleDocumentType<TDocument>(TypermediaHandlerRegistrarWithDependencyInjectionSupport typermediaRegistrar) where TDocument : class
   {
      DocumentDbApi.HandleDocumentType<TDocument>(typermediaRegistrar);
      return this;
   }
}
