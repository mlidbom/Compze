using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Serialization.Newtonsoft.Private.DocumentDb;

static class NewtonsoftDocumentDbSerializerRegistrar
{
   public static IComponentRegistrar NewtonsoftDocumentDbSerializer(this IComponentRegistrar registrar) =>
      DocumentDb.NewtonsoftDocumentDbSerializer.RegisterWith(registrar);
}

class NewtonsoftDocumentDbSerializer : RenamingSupportingJsonSerializer, IDocumentDbSerializer
{
   public static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IDocumentDbSerializer>()
                                     .CreatedBy((ITypeIdentifierMapper typeMapper) => new NewtonsoftDocumentDbSerializer(typeMapper)));

   NewtonsoftDocumentDbSerializer(ITypeIdentifierMapper typeMapper) : base(RenamingAndNonPublicMembersSupportingJsonSettings.DocumentDb, typeMapper) {}
}
