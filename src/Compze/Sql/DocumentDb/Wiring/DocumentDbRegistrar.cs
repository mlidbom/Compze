using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Serialization.Newtonsoft;
using Compze.Serialization.Newtonsoft.Private.DocumentDb;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.DocumentDb.Wiring;

public static class DocumentDbRegistrar
{
   public static DocumentDbRegistrationBuilder RegisterDocumentDb(this IEndpointBuilder @this)
      => @this.Container.Register().DocumentDb();

   public static DocumentDbRegistrationBuilder DocumentDb(this IComponentRegistrar registrar)
   {
      registrar.Register(Compze.DocumentDb.DocumentDb.RegisterWith,
                         it => NewtonsoftDocumentDbSerializer.RegisterWith(it),
                         DocumentDbSession.RegisterWith);

      return new DocumentDbRegistrationBuilder();
   }
}

public class DocumentDbRegistrationBuilder
{
   public DocumentDbRegistrationBuilder HandleDocumentType<TDocument>(TessageHandlerRegistrarWithDependencyInjectionSupport registrar)
   {
      DocumentDbApi.HandleDocumentType<TDocument>(registrar);
      return this;
   }
}
