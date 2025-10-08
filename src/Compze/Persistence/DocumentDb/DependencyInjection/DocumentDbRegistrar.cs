using Compze.Abstractions.Internal.Persistence.DocumentDb;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Abstractions.Internal.Time;
using Compze.Persistence.DocumentDb.Abstractions;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Persistence.DocumentDb.DependencyInjection;

public static class DocumentDbRegistrar
{
   public static DocumentDbRegistrationBuilder RegisterDocumentDb(this IEndpointBuilder @this)
      => @this.Container.RegisterDocumentDb(@this.Configuration.ConnectionStringName);

   public static DocumentDbRegistrationBuilder RegisterDocumentDb(this IDependencyInjectionContainer @this, string connectionName)
   {
      Assert.Argument.NotNullEmptyOrWhitespace(connectionName);

      var registrar = @this.Register()
                           .Register(DocumentDbSerializer.RegisterWith,
                                     DocumentDb.RegisterWith,
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
