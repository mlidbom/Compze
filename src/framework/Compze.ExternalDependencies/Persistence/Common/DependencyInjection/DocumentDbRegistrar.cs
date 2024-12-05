using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.GenericAbstractions.Time;
using Compze.Messaging.Buses;
using Compze.Persistence.DocumentDb;
using Compze.Refactoring.Naming;
using Compze.Serialization;

namespace Compze.Persistence.Common.DependencyInjection;

public static class DocumentDbRegistrar
{
   public static DocumentDbRegistrationBuilder RegisterDocumentDb(this IEndpointBuilder @this)
      => @this.Container.RegisterDocumentDb(@this.Configuration.ConnectionStringName);

   public static DocumentDbRegistrationBuilder RegisterDocumentDb(this IDependencyInjectionContainer @this, string connectionName)
   {
      Assert.Argument.NotNullEmptyOrWhitespace(connectionName);

      @this.Register(Singleton.For<IDocumentDbSerializer>()
                              .CreatedBy((ITypeMapper typeMapper) => new DocumentDbSerializer(typeMapper)));

      @this.Register(Scoped.For<IDocumentDb>()
                           .CreatedBy((IDocumentDbPersistenceLayer persistenceLayer, ITypeMapper typeMapper, IUtcTimeTimeSource timeSource, IDocumentDbSerializer serializer)
                                         => new DocumentDb.DocumentDb(timeSource, serializer, typeMapper, persistenceLayer)));

      @this.Register(Scoped.For<IDocumentDbSession, IDocumentDbUpdater, IDocumentDbReader, IDocumentDbBulkReader>()
                           .CreatedBy((IDocumentDb documentDb) => new DocumentDbSession(documentDb)));

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