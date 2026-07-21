using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization._internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Serialization.Newtonsoft._private.DocumentDb;

class NewtonsoftDocumentDbSerializer : RenamingSupportingJsonSerializer, IDocumentDbSerializer
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IDocumentDbSerializer>()
                                     .CreatedBy((ITypeMap typeMap) => new NewtonsoftDocumentDbSerializer(typeMap)));

   NewtonsoftDocumentDbSerializer(ITypeMap typeMap) : base(RenamingAndNonPublicMembersSupportingJsonSettings.DocumentDb, typeMap) {}
}
