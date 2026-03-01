using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Serialization.Newtonsoft.Private.DocumentDb;

internal static class NewtonsoftDocumentDbSerializerRegistrar
{
   public static IComponentRegistrar NewtonsoftDocumentDbSerializer(this IComponentRegistrar registrar) =>
      DocumentDb.NewtonsoftDocumentDbSerializer.RegisterWith(registrar);
}

internal class NewtonsoftDocumentDbSerializer : RenamingSupportingJsonSerializer, IDocumentDbSerializer
{
   public static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IDocumentDbSerializer>()
                                     .CreatedBy((ITypeMapper typeMapper) => new NewtonsoftDocumentDbSerializer(typeMapper)));

   NewtonsoftDocumentDbSerializer(ITypeMapper typeMapper) : base(RenamingAndNonPublicMembersSupportingJsonSettings.DocumentDb, typeMapper) {}
}
