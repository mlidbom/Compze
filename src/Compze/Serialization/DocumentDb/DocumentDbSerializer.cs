using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Serialization.Newtonsoft.DocumentDb;

class DocumentDbSerializer : RenamingSupportingJsonSerializer, IDocumentDbSerializer
{
   DocumentDbSerializer(ITypeMapper typeMapper) : base(RenamingAndNonPublicMembersSupportingJSONSettings.TeventStore, typeMapper) {}

   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IDocumentDbSerializer>()
                                     .CreatedBy((ITypeMapper typeMapper) => new DocumentDbSerializer(typeMapper)));
}
